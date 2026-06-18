namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Abstrakcja katalogu sklepu. Reszta aplikacji zależy tylko od tego interfejsu —
/// konkretny sklep (np. Carrefour) jest wymienialny. Patrz README → „Architektura".
/// </summary>
public interface ICatalogProvider
{
    /// <summary>Wyszukuje produkty po frazie; zwraca stronę wyników.</summary>
    Task<SearchResult> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Pobiera pojedynczy produkt po kodzie (EAN / SKU / id sklepu). <c>null</c> gdy brak.</summary>
    Task<Product?> GetProductAsync(string code, CancellationToken cancellationToken = default);
}
