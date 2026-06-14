using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Providers.Frisco;

namespace ZakupOgarniacz.Api.Endpoints;

/// <summary>Endpointy koszyka: tworzenie, podgląd, dodawanie pozycji, eksport.</summary>
public static class CartEndpoints
{
    /// <summary>Żądanie dodania pozycji do koszyka.</summary>
    public sealed record AddItemRequest(string Code, int Quantity = 1);

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
}
