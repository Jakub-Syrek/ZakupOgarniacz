using System.Text.Json;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.Carrefour;

/// <summary>
/// Adapter <see cref="ICatalogProvider"/> dla Carrefour. Sięga do wewnętrznego API
/// <c>/web/catalog</c> przez <see cref="ICarrefourTransport"/> (sesja przeglądarki, za Cloudflare)
/// i mapuje wynik na modele domenowe.
/// </summary>
public sealed class CarrefourProvider : ICatalogProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICarrefourTransport _transport;
    private readonly Uri _baseUri;

    public CarrefourProvider(ICarrefourTransport transport, CarrefourOptions options)
    {
        _transport = transport;
        _baseUri = new Uri(options.BaseUrl);
    }

    public async Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Carrefour /web/catalog jest stronicowane w stylu Spring Data (page 0-based + size).
        // Param strony do potwierdzenia na żywo — stąd page-1.
        var path =
            $"web/catalog?search={Uri.EscapeDataString(query)}&size={pageSize}&page={Math.Max(0, page - 1)}";

        var json = await _transport.GetJsonAsync(path, cancellationToken);
        var dto = JsonSerializer.Deserialize<CarrefourCatalogResponse>(json, JsonOptions);

        return CarrefourCatalogMapper.ToSearchResult(dto, page, pageSize, _baseUri);
    }

    public async Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default)
    {
        // Brak dedykowanego endpointu produktu z reconu — na razie szukamy po kodzie i dopasowujemy.
        var result = await SearchAsync(code, page: 1, pageSize: 5, cancellationToken);

        return result.Items.FirstOrDefault(p => p.Ean == code || p.Sku == code || p.Id == code)
               ?? result.Items.FirstOrDefault();
    }
}
