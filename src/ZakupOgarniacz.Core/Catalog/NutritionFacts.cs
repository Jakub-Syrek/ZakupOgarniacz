namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Wartości odżywcze w przeliczeniu na 100 g / 100 ml produktu.
/// Każde pole może być <c>null</c>, jeśli źródło danych nie podaje wartości.
/// </summary>
public sealed record NutritionFacts(
    double? EnergyKcalPer100g,
    double? FatPer100g,
    double? SaturatedFatPer100g,
    double? CarbohydratesPer100g,
    double? SugarsPer100g,
    double? FiberPer100g,
    double? ProteinsPer100g,
    double? SaltPer100g,
    double? SodiumPer100g)
{
    /// <summary>Pusty zestaw wartości — gdy produkt nie ma żadnych danych żywieniowych.</summary>
    public static NutritionFacts Empty { get; } =
        new(null, null, null, null, null, null, null, null, null);
}
