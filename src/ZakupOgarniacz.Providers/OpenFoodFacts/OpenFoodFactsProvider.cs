using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.OpenFoodFacts;

/// <summary>
/// Adapter <see cref="ICatalogProvider"/> oparty o publiczne API Open Food Facts.
/// Używa wstrzykniętego (typed) <see cref="HttpClient"/> — bazowy adres i nagłówki
/// konfiguruje warstwa Infrastructure.
/// </summary>
public sealed class OpenFoodFactsProvider : ICatalogProvider
{
    // Pola pobierane ze źródła — ograniczamy payload tylko do tego, czego używamy.
    private const string SearchFields = "code,product_name,brands,image_url,image_front_url,nutriscore_grade";
    private const string ProductFields =
        "code,product_name,brands,quantity,image_front_url,image_url,categories," +
        "ingredients_text,allergens_tags,nutriscore_grade,nutriments";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public OpenFoodFactsProvider(HttpClient http) => _http = http;

    public async Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"cgi/search.pl?search_terms={Uri.EscapeDataString(query)}" +
            $"&search_simple=1&action=process&json=1&page={page}&page_size={pageSize}&fields={SearchFields}";

        var response = await _http.GetFromJsonAsync<OffSearchResponse>(url, JsonOptions, cancellationToken);

        var items = (response?.Products ?? [])
            .Select(OffMapper.ToSummary)
            .ToList();

        return new SearchResult(items, page, pageSize, response?.Count ?? items.Count);
    }

    public async Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/product/{Uri.EscapeDataString(code)}.json?fields={ProductFields}";

        using var httpResponse = await _http.GetAsync(url, cancellationToken);
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content
            .ReadFromJsonAsync<OffProductResponse>(JsonOptions, cancellationToken);

        if (response?.Product is null || response.Status == 0)
        {
            return null;
        }

        return OffMapper.ToProduct(response.Product);
    }

    public async Task<NutritionFacts?> GetNutritionAsync(string code, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(code, cancellationToken);
        return product?.Nutrition;
    }
}
