namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Strona wyników wyszukiwania produktów wraz z informacją o paginacji.
/// </summary>
public sealed record SearchResult(
    IReadOnlyList<ProductSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);
