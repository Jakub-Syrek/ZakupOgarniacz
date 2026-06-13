using System.Globalization;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>Mapowanie odpowiedzi Frisco <c>/offer/products/query</c> na modele z Core.</summary>
internal static class FriscoCatalogMapper
{
    public static SearchResult ToSearchResult(FriscoQueryResponse? response, int requestedPageSize)
    {
        var items = (response?.Products ?? [])
            .Select(ToProduct)
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        return new SearchResult(
            items,
            Page: response?.PageIndex ?? 1,
            PageSize: response?.PageSize ?? requestedPageSize,
            TotalCount: response?.TotalCount ?? items.Count);
    }

    public static Product? ToProduct(FriscoOfferItem item)
    {
        var p = item.Product;
        if (p is null)
        {
            return null;
        }

        return new Product(
            Id: NullIfBlank(p.ProductId) ?? NullIfBlank(p.Id) ?? NullIfBlank(item.ProductId) ?? string.Empty,
            Name: NullIfBlank(p.Name?.Pl) ?? NullIfBlank(p.Name?.En) ?? string.Empty,
            Brand: NullIfBlank(p.Brand) ?? NullIfBlank(p.Producer),
            Ean: NullIfBlank(p.Ean),
            Sku: NullIfBlank(p.ProductId),
            Size: FormatSize(p.Grammage, p.UnitOfMeasure),
            ImageUrl: NullIfBlank(p.ImageUrl),
            ProductUrl: null, // odpowiedź nie zawiera slug-a strony produktu
            Categories: MapCategories(p.Categories),
            Price: MapPrice(p.Price),
            Available: p.IsAvailable ?? false)
        {
            PromotionalPrice = MapPromotion(p.Price),
        };
    }

    private static Money? MapPrice(FriscoPriceDto? price)
    {
        var amount = price?.Price ?? price?.PriceAfterPromotion;
        return amount is { } value ? new Money(value) : null;
    }

    // Promocja tylko, gdy cena po promocji jest realnie niższa od regularnej.
    private static Money? MapPromotion(FriscoPriceDto? price)
    {
        if (price?.PriceAfterPromotion is not { } promo)
        {
            return null;
        }

        return price.Price is { } regular && promo >= regular ? null : new Money(promo);
    }

    private static IReadOnlyList<string> MapCategories(List<FriscoCategoryDto>? categories)
    {
        if (categories is null || categories.Count == 0)
        {
            return [];
        }

        return categories
            .OrderBy(c => c.Depth ?? 0)
            .Select(c => NullIfBlank(c.Name?.Pl))
            .Where(n => n is not null)
            .Select(n => n!)
            .Distinct()
            .ToList();
    }

    private static string? FormatSize(decimal? grammage, string? unitOfMeasure)
    {
        if (grammage is not { } value)
        {
            return NullIfBlank(unitOfMeasure);
        }

        var unit = unitOfMeasure?.ToLowerInvariant() switch
        {
            "kilogram" => "kg",
            "liter" or "litre" => "l",
            "piece" or "sztuka" => "szt.",
            _ => NullIfBlank(unitOfMeasure),
        };

        var number = value.ToString("0.###", CultureInfo.InvariantCulture);
        return unit is null ? number : $"{number} {unit}";
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
