using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.IntegrationTests.TestDoubles;

/// <summary>
/// Atrapa katalogu z deterministycznymi danymi — podmieniana za realny adapter Carrefour,
/// żeby testy endpointów nie odpalały przeglądarki (Playwright) ani sieci.
/// Produkt o kodzie <c>111</c> istnieje; pozostałe zwracają <c>null</c>.
/// </summary>
internal sealed class FakeCatalogProvider : ICatalogProvider
{
    public const string KnownCode = "111";

    private static Product Sample() => new(
        Id: KnownCode,
        Name: "Mleko UHT 2% Łaciate 1 l",
        Brand: "Łaciate",
        Ean: "5900512300108",
        Sku: "SKU111",
        Size: "1 l",
        ImageUrl: "https://www.carrefour.pl/img/111.jpg",
        ProductUrl: "https://www.carrefour.pl/produkt/111",
        Categories: ["Nabiał", "Mleko"],
        Price: null,
        Available: true);

    public Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SearchResult([Sample()], page, pageSize, 1));

    public Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(code == KnownCode ? Sample() : null);
}
