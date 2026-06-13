using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZakupOgarniacz.Providers.OpenFoodFacts;

// Wewnętrzne DTO odwzorowujące surowe JSON-y Open Food Facts.
// Mapowane na modele domenowe z Core przez OffMapper — nie wychodzą poza warstwę Providers.

/// <summary>Odpowiedź endpointu wyszukiwania (cgi/search.pl).</summary>
internal sealed class OffSearchResponse
{
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("products")]
    public List<OffProduct>? Products { get; set; }
}

/// <summary>Odpowiedź endpointu pojedynczego produktu (api/v2/product/{code}.json).</summary>
internal sealed class OffProductResponse
{
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("product")]
    public OffProduct? Product { get; set; }
}

/// <summary>Surowy produkt Open Food Facts (wspólny dla wyszukiwania i szczegółów).</summary>
internal sealed class OffProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; set; }

    [JsonPropertyName("allergens_tags")]
    public List<string>? AllergensTags { get; set; }

    [JsonPropertyName("nutriscore_grade")]
    public string? NutriScoreGrade { get; set; }

    // Klucze odżywcze mają myślniki (np. "saturated-fat_100g") i bywają liczbami
    // lub stringami — trzymamy je jako JsonElement i wyciągamy bezpiecznie w mapperze.
    [JsonPropertyName("nutriments")]
    public Dictionary<string, JsonElement>? Nutriments { get; set; }
}
