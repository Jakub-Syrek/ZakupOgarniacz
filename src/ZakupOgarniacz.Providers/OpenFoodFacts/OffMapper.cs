using System.Globalization;
using System.Text.Json;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.OpenFoodFacts;

/// <summary>Mapowanie surowych DTO Open Food Facts na modele domenowe z Core.</summary>
internal static class OffMapper
{
    public static ProductSummary ToSummary(OffProduct p) => new(
        Code: p.Code ?? string.Empty,
        Name: NullIfBlank(p.ProductName),
        Brand: NullIfBlank(p.Brands),
        ImageUrl: NullIfBlank(p.ImageFrontUrl) ?? NullIfBlank(p.ImageUrl),
        NutriScore: ParseNutriScore(p.NutriScoreGrade));

    public static Product ToProduct(OffProduct p) => new(
        Code: p.Code ?? string.Empty,
        Name: NullIfBlank(p.ProductName),
        Brand: NullIfBlank(p.Brands),
        Quantity: NullIfBlank(p.Quantity),
        ImageUrl: NullIfBlank(p.ImageFrontUrl) ?? NullIfBlank(p.ImageUrl),
        Categories: SplitCategories(p.Categories),
        Ingredients: NullIfBlank(p.IngredientsText),
        Allergens: MapAllergens(p.AllergensTags),
        NutriScore: ParseNutriScore(p.NutriScoreGrade),
        Nutrition: MapNutrition(p.Nutriments));

    private static NutritionFacts MapNutrition(Dictionary<string, JsonElement>? n)
    {
        if (n is null || n.Count == 0)
        {
            return NutritionFacts.Empty;
        }

        double? Get(string key) => n.TryGetValue(key, out var element) ? ToDouble(element) : null;

        return new NutritionFacts(
            EnergyKcalPer100g: Get("energy-kcal_100g"),
            FatPer100g: Get("fat_100g"),
            SaturatedFatPer100g: Get("saturated-fat_100g"),
            CarbohydratesPer100g: Get("carbohydrates_100g"),
            SugarsPer100g: Get("sugars_100g"),
            FiberPer100g: Get("fiber_100g"),
            ProteinsPer100g: Get("proteins_100g"),
            SaltPer100g: Get("salt_100g"),
            SodiumPer100g: Get("sodium_100g"));
    }

    private static double? ToDouble(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number when element.TryGetDouble(out var d) => d,
        JsonValueKind.String when double.TryParse(
            element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
        _ => null,
    };

    private static NutriScore ParseNutriScore(string? grade) => grade?.Trim().ToLowerInvariant() switch
    {
        "a" => NutriScore.A,
        "b" => NutriScore.B,
        "c" => NutriScore.C,
        "d" => NutriScore.D,
        "e" => NutriScore.E,
        _ => NutriScore.Unknown,
    };

    // "en:milk" / "fr:lait" -> "milk" / "lait" (ucinamy prefiks języka).
    private static IReadOnlyList<string> MapAllergens(List<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return [];
        }

        var result = new List<string>(tags.Count);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var colon = tag.IndexOf(':');
            result.Add(colon >= 0 ? tag[(colon + 1)..] : tag);
        }

        return result;
    }

    private static IReadOnlyList<string> SplitCategories(string? categories)
    {
        if (string.IsNullOrWhiteSpace(categories))
        {
            return [];
        }

        return categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
