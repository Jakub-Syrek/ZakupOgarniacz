namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Pełny produkt spożywczy z danymi żywieniowymi. Model niezależny od źródła
/// (mapowany z konkretnego dostawcy przez <see cref="ICatalogProvider"/>).
/// </summary>
public sealed record Product(
    string Code,
    string? Name,
    string? Brand,
    string? Quantity,
    string? ImageUrl,
    IReadOnlyList<string> Categories,
    string? Ingredients,
    IReadOnlyList<string> Allergens,
    NutriScore NutriScore,
    NutritionFacts Nutrition);
