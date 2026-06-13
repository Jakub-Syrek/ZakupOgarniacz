using System.Net.Http.Json;
using System.Text.Json;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Adapter <see cref="ICatalogProvider"/> dla Frisco.pl. Sięga do publicznego JSON API
/// <c>/offer/products/query</c> zwykłym <see cref="HttpClient"/> (brak Cloudflare → bez
/// przeglądarki). Cena i dostępność są już w odpowiedzi wyszukiwania.
/// </summary>
public sealed class FriscoProvider : ICatalogProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public FriscoProvider(HttpClient http) => _http = http;

    public async Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"offer/products/query?query={Uri.EscapeDataString(query)}&pageIndex={Math.Max(1, page)}&pageSize={pageSize}";

        var response = await _http.GetFromJsonAsync<FriscoQueryResponse>(url, JsonOptions, cancellationToken);

        return FriscoCatalogMapper.ToSearchResult(response, pageSize);
    }

    public async Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default)
    {
        // Brak dedykowanego pojedynczego endpointu w użyciu — szukamy po kodzie i dopasowujemy.
        var result = await SearchAsync(code, page: 1, pageSize: 10, cancellationToken);

        return result.Items.FirstOrDefault(p => p.Ean == code || p.Id == code || p.Sku == code)
               ?? result.Items.FirstOrDefault();
    }
}
