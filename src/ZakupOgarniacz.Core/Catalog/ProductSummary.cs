namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Lekki rekord produktu na listę wyników wyszukiwania (bez pełnych danych żywieniowych).
/// </summary>
public sealed record ProductSummary(
    string Code,
    string? Name,
    string? Brand,
    string? ImageUrl,
    NutriScore NutriScore);
