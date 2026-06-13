using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;

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

        return app;
    }
}
