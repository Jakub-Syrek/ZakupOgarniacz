using System.Text.Json.Serialization;

namespace ZakupOgarniacz.Providers.Frisco;

// DTO odpowiedzi GET /app/commerce/api/v1/offer/products/query?query=...
// Kształt ustalony rekonesansem; mapowane na Core przez FriscoCatalogMapper.

internal sealed class FriscoQueryResponse
{
    [JsonPropertyName("pageIndex")]
    public int PageIndex { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("products")]
    public List<FriscoOfferItem>? Products { get; set; }
}

internal sealed class FriscoOfferItem
{
    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }

    [JsonPropertyName("product")]
    public FriscoProductDto? Product { get; set; }
}

internal sealed class FriscoProductDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }

    [JsonPropertyName("ean")]
    public string? Ean { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("producer")]
    public string? Producer { get; set; }

    [JsonPropertyName("grammage")]
    public decimal? Grammage { get; set; }

    [JsonPropertyName("unitOfMeasure")]
    public string? UnitOfMeasure { get; set; }

    [JsonPropertyName("name")]
    public FriscoLocalized? Name { get; set; }

    [JsonPropertyName("categories")]
    public List<FriscoCategoryDto>? Categories { get; set; }

    [JsonPropertyName("isAvailable")]
    public bool? IsAvailable { get; set; }

    [JsonPropertyName("price")]
    public FriscoPriceDto? Price { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}

internal sealed class FriscoLocalized
{
    [JsonPropertyName("pl")]
    public string? Pl { get; set; }

    [JsonPropertyName("en")]
    public string? En { get; set; }
}

internal sealed class FriscoCategoryDto
{
    [JsonPropertyName("depth")]
    public int? Depth { get; set; }

    [JsonPropertyName("name")]
    public FriscoLocalized? Name { get; set; }
}

internal sealed class FriscoPriceDto
{
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("priceAfterPromotion")]
    public decimal? PriceAfterPromotion { get; set; }
}
