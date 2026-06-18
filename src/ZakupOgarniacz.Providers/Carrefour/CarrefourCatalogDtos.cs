using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZakupOgarniacz.Providers.Carrefour;

// Wewnętrzne DTO odwzorowujące odpowiedź GET /web/catalog?search=...
// Kształt ustalony rekonesansem; mapowane na modele z Core przez CarrefourCatalogMapper.

internal sealed class CarrefourCatalogResponse
{
    [JsonPropertyName("content")]
    public List<CarrefourCatalogItem>? Content { get; set; }

    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int? TotalPages { get; set; }
}

internal sealed class CarrefourCatalogItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("brandName")]
    public string? BrandName { get; set; }

    [JsonPropertyName("actualSku")]
    public string? ActualSku { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("active")]
    public bool? Active { get; set; }

    [JsonPropertyName("defaultCategoryName")]
    public string? DefaultCategoryName { get; set; }

    [JsonPropertyName("productCategories")]
    public List<CarrefourCategory>? ProductCategories { get; set; }

    // Obraz bywa stringiem (URL) albo obiektem z wariantami rozmiarów — trzymamy jako
    // JsonElement i wyciągamy URL defensywnie w mapperze.
    [JsonPropertyName("defaultImage")]
    public JsonElement DefaultImage { get; set; }

    [JsonPropertyName("images")]
    public JsonElement Images { get; set; }

    [JsonPropertyName("product")]
    public CarrefourProductDetail? Product { get; set; }
}

internal sealed class CarrefourCategory
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class CarrefourProductDetail
{
    [JsonPropertyName("code")]
    public string? Code { get; set; } // kod / EAN

    [JsonPropertyName("sizeWithUnitString")]
    public string? SizeWithUnitString { get; set; }

    [JsonPropertyName("sellUnitString")]
    public string? SellUnitString { get; set; }

    [JsonPropertyName("grammageUnit")]
    public string? GrammageUnit { get; set; }
}
