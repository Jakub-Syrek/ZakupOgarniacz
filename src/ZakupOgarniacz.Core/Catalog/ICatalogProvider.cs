namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Abstrakcja źródła danych o produktach spożywczych. Reszta aplikacji zależy
/// tylko od tego interfejsu — konkretny dostawca (np. Open Food Facts) jest
/// wymienialny. Patrz README → „Architektura".
/// </summary>
public interface ICatalogProvider
{
    /// <summary>Wyszukuje produkty po frazie tekstowej; zwraca stronę wyników.</summary>
    Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Pobiera pełny produkt po kodzie (kod kreskowy / barcode). <c>null</c> gdy nie znaleziono.</summary>
    Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Pobiera same wartości odżywcze produktu. <c>null</c> gdy produkt nie istnieje.</summary>
    Task<NutritionFacts?> GetNutritionAsync(string code, CancellationToken cancellationToken = default);
}
