using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Core.Shopping;
using ZakupOgarniacz.Providers.Frisco;

namespace ZakupOgarniacz.Api.Endpoints;

/// <summary>Endpointy koszyka: tworzenie, podgląd, dodawanie pozycji, eksport.</summary>
public static class CartEndpoints
{
    /// <summary>Żądanie dodania pozycji do koszyka.</summary>
    public sealed record AddItemRequest(string Code, int Quantity = 1);

    /// <summary>Żądanie zbudowania koszyka z polecenia w naturalnym języku.</summary>
    public sealed record FromCommandRequest(string Command, string? CartId, IReadOnlyList<string>? Preferences = null);

    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // Tworzenie koszyka mapujemy jawnie (unikamy pułapki końcowego ukośnika w grupie).
        app.MapPost("/carts", async (ICartStore store, CancellationToken cancellationToken) =>
        {
            var cart = await store.CreateAsync(cancellationToken);
            return Results.Created($"/carts/{cart.Id}", cart);
        })
        .WithTags("Cart")
        .WithName("CreateCart");

        // Polecenie w naturalnym języku → parsowanie (Claude) → wyszukanie w katalogu → koszyk.
        app.MapPost("/carts/from-command", async (
            FromCommandRequest request,
            IShoppingListParser parser,
            ICatalogProvider catalog,
            ICartStore store,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return Results.BadRequest(new { error = "Pole 'command' jest wymagane." });
            }

            if (!parser.IsConfigured)
            {
                return Results.Problem("Brak klucza Anthropic API (sekcja Claude / ANTHROPIC_API_KEY).", statusCode: 503);
            }

            var preferences = (request.Preferences ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();

            IReadOnlyList<ShoppingItem> items;
            try
            {
                items = await parser.ParseAsync(request.Command, preferences, cancellationToken);
            }
            catch (Exception ex)
            {
                return Results.Problem("Parser: " + ex.Message, statusCode: 502);
            }

            var cart = (request.CartId is { } id ? await store.GetAsync(id, cancellationToken) : null)
                       ?? await store.CreateAsync(cancellationToken);

            var report = new List<object>();
            foreach (var item in items)
            {
                var result = await catalog.SearchAsync(item.Query, 1, 5, cancellationToken);
                // Faworyzujemy produkt pasujący do ulubionych (marka/nazwa), w obrębie dostępnych.
                var available = result.Items.Where(p => p.Available).ToList();
                var pool = available.Count > 0 ? available : result.Items.ToList();
                var product = pool.FirstOrDefault(p => MatchesPreference(p, preferences)) ?? pool.FirstOrDefault();
                if (product is not null)
                {
                    cart.Add(product, item.Quantity);
                    report.Add(new { query = item.Query, quantity = item.Quantity, matched = product.Name, productId = product.Id, price = product.Price });
                }
                else
                {
                    report.Add(new { query = item.Query, quantity = item.Quantity, matched = (string?)null });
                }
            }

            await store.SaveAsync(cart, cancellationToken);
            return Results.Ok(new { cartId = cart.Id, cart, report });
        })
        .WithTags("Cart")
        .WithName("CartFromCommand");

        var group = app.MapGroup("/carts").WithTags("Cart");

        group.MapGet("/{cartId}", async (string cartId, ICartStore store, CancellationToken cancellationToken) =>
        {
            var cart = await store.GetAsync(cartId, cancellationToken);
            return cart is null ? Results.NotFound() : Results.Ok(cart);
        })
        .WithName("GetCart");

        group.MapPost("/{cartId}/items", async (
            string cartId,
            AddItemRequest request,
            ICartStore store,
            ICatalogProvider catalog,
            CancellationToken cancellationToken) =>
        {
            if (request.Quantity < 1)
            {
                return Results.BadRequest(new { error = "Quantity musi być >= 1." });
            }

            var cart = await store.GetAsync(cartId, cancellationToken);
            if (cart is null)
            {
                return Results.NotFound();
            }

            var product = await catalog.GetProductAsync(request.Code, cancellationToken);
            if (product is null)
            {
                return Results.BadRequest(new { error = $"Nie znaleziono produktu: {request.Code}." });
            }

            cart.Add(product, request.Quantity);
            await store.SaveAsync(cart, cancellationToken);
            return Results.Ok(cart);
        })
        .WithName("AddCartItem");

        group.MapPost("/{cartId}/clear", async (string cartId, ICartStore store, CancellationToken cancellationToken) =>
        {
            var cart = await store.GetAsync(cartId, cancellationToken);
            if (cart is null)
            {
                return Results.NotFound();
            }

            cart.Clear();
            await store.SaveAsync(cart, cancellationToken);
            return Results.Ok(cart);
        })
        .WithName("ClearCart");

        group.MapPost("/{cartId}/export", async (
            string cartId,
            ICartStore store,
            IOrderProvider orders,
            CancellationToken cancellationToken) =>
        {
            var cart = await store.GetAsync(cartId, cancellationToken);
            if (cart is null)
            {
                return Results.NotFound();
            }

            var export = await orders.ExportCartAsync(cart, cancellationToken);
            return Results.Ok(export);
        })
        .WithName("ExportCart");

        // Auto-checkout (etap): wrzuca pozycje naszego koszyka do koszyka Frisco (po tokenie).
        // Granica: NIE realizuje płatności — to robisz w Frisco.
        group.MapPost("/{cartId}/push-to-frisco", async (
            string cartId,
            ICartStore store,
            FriscoCheckoutClient frisco,
            CancellationToken cancellationToken) =>
        {
            if (!frisco.IsConfigured)
            {
                return Results.Problem("Brak poświadczeń Frisco (ustaw token w sekcji Auto-checkout).", statusCode: 503);
            }

            var cart = await store.GetAsync(cartId, cancellationToken);
            if (cart is null)
            {
                return Results.NotFound();
            }

            if (cart.Items.Count == 0)
            {
                return Results.BadRequest(new { error = "Koszyk jest pusty." });
            }

            var items = cart.Items.Select(i => (i.Product.Id, i.Quantity)).ToList();
            try
            {
                var result = await frisco.AddProductsAsync(items, cancellationToken);
                return Results.Json(
                    new { friscoStatus = result.StatusCode, ok = result.IsSuccess, count = items.Count, route = result.Route },
                    statusCode: result.IsSuccess ? 200 : 502);
            }
            catch (Exception ex)
            {
                return Results.Problem("Push do Frisco: " + ex.Message, statusCode: 502);
            }
        })
        .WithName("PushCartToFrisco");

        return app;
    }

    /// <summary>Czy produkt pasuje do którejś z preferencji (po nazwie lub marce, bez rozróżniania wielkości liter).</summary>
    private static bool MatchesPreference(Product product, IReadOnlyList<string> preferences)
    {
        if (preferences.Count == 0)
        {
            return false;
        }

        foreach (var pref in preferences)
        {
            if (product.Name.Contains(pref, StringComparison.OrdinalIgnoreCase)
                || (product.Brand is { } brand && brand.Contains(pref, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
