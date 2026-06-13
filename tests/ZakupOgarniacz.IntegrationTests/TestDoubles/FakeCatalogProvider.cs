using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.IntegrationTests.TestDoubles;

/// <summary>
/// Atrapa katalogu z deterministycznymi danymi — podmieniana za realny adapter
/// Open Food Facts, żeby testy integracyjne nie odpytywały sieci.
/// Produkt o kodzie <c>111</c> istnieje; pozostałe kody zwracają <c>null</c>.
/// </summary>
internal sealed class FakeCatalogProvider : ICatalogProvider
{
    public const string KnownCode = "111";

    public Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var item = new ProductSummary(KnownCode, "Mleko 2%", "Łaciate", "https://img/111.jpg", NutriScore.B);
        return Task.FromResult(new SearchResult([item], page, pageSize, 1));
    }

    public Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default)
    {
        if (code != KnownCode)
        {
            return Task.FromResult<Product?>(null);
        }

        var product = new Product(
            Code: KnownCode,
            Name: "Mleko 2%",
            Brand: "Łaciate",
            Quantity: "1 l",
            ImageUrl: "https://img/111-front.jpg",
            Categories: ["Dairies", "Milks"],
            Ingredients: "mleko",
            Allergens: ["milk"],
            NutriScore: NutriScore.B,
            Nutrition: new NutritionFacts(50, 2, 1.3, 4.8, 4.8, null, 3.2, 0.1, null));

        return Task.FromResult<Product?>(product);
    }

    public async Task<NutritionFacts?> GetNutritionAsync(string code, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(code, cancellationToken);
        return product?.Nutrition;
    }
}
