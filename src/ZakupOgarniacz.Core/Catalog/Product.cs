namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Produkt z katalogu sklepu. Model niezależny od sklepu — mapowany z konkretnego
/// dostawcy przez <see cref="ICatalogProvider"/>.
/// </summary>
/// <remarks>
/// <see cref="Price"/> bywa <c>null</c>: w Carrefour cena zależy od wybranego sklepu/dostawy
/// i nie jest zwracana przez samo wyszukiwanie katalogu (dochodzi po ustaleniu kontekstu sklepu).
/// </remarks>
public sealed record Product(
    string Id,
    string Name,
    string? Brand,
    string? Ean,
    string? Sku,
    string? Size,
    string? ImageUrl,
    string? ProductUrl,
    IReadOnlyList<string> Categories,
    Money? Price,
    bool Available);
