using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Api.Endpoints;

/// <summary>Endpointy read-only katalogu produktów (krok 1 roadmapy).</summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Catalog");

        group.MapGet("/search", async (
            string q,
            ICatalogProvider catalog,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.BadRequest(new { error = "Parametr 'q' jest wymagany." });
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await catalog.SearchAsync(q, page, pageSize, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("SearchProducts");

        group.MapGet("/{code}", async (string code, ICatalogProvider catalog, CancellationToken cancellationToken) =>
        {
            var product = await catalog.GetProductAsync(code, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        })
        .WithName("GetProduct");

        group.MapGet("/{code}/nutrition", async (string code, ICatalogProvider catalog, CancellationToken cancellationToken) =>
        {
            var nutrition = await catalog.GetNutritionAsync(code, cancellationToken);
            return nutrition is null ? Results.NotFound() : Results.Ok(nutrition);
        })
        .WithName("GetProductNutrition");

        return app;
    }
}
