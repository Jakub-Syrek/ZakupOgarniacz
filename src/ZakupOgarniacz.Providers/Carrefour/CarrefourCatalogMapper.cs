using System.Text.Json;
using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Providers.Carrefour;

/// <summary>Mapowanie odpowiedzi <c>/web/catalog</c> na modele domenowe z Core.</summary>
internal static class CarrefourCatalogMapper
{
    public static SearchResult ToSearchResult(
        CarrefourCatalogResponse? response,
        int page,
        int pageSize,
        Uri baseUri)
    {
        var items = (response?.Content ?? [])
            .Select(item => ToProduct(item, baseUri))
            .ToList();

        return new SearchResult(items, page, pageSize, response?.TotalCount ?? items.Count);
    }

    public static Product ToProduct(CarrefourCatalogItem item, Uri baseUri) => new(
        Id: item.Id ?? item.ActualSku ?? string.Empty,
        Name: NullIfBlank(item.DisplayName) ?? NullIfBlank(item.Name) ?? string.Empty,
        Brand: NullIfBlank(item.BrandName),
        Ean: NullIfBlank(item.Product?.Code),
        Sku: NullIfBlank(item.ActualSku),
        Size: NullIfBlank(item.Product?.SizeWithUnitString),
        ImageUrl: ResolveUrl(ExtractImageUrl(item.DefaultImage) ?? ExtractImageUrl(item.Images), baseUri),
        ProductUrl: ResolveUrl(NullIfBlank(item.Url) ?? NullIfBlank(item.Slug), baseUri),
        Categories: MapCategories(item),
        Price: null, // cena niedostępna w wyszukiwaniu — zależy od wybranego sklepu
        Available: item.Active ?? true);

    private static IReadOnlyList<string> MapCategories(CarrefourCatalogItem item)
    {
        var fromList = (item.ProductCategories ?? [])
            .Select(c => NullIfBlank(c.Name))
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();

        if (fromList.Count > 0)
        {
            return fromList;
        }

        var fallback = NullIfBlank(item.DefaultCategoryName);
        return fallback is null ? [] : [fallback];
    }

    private static string? ExtractImageUrl(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return NullIfBlank(element.GetString());
            case JsonValueKind.Array when element.GetArrayLength() > 0:
                return ExtractImageUrl(element[0]);
            case JsonValueKind.Object:
                foreach (var key in new[] { "url", "src", "path", "large", "medium", "default", "small" })
                {
                    if (element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        return NullIfBlank(value.GetString());
                    }
                }

                return null;
            default:
                return null;
        }
    }

    private static string? ResolveUrl(string? value, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(baseUri, value, out var absolute) ? absolute.ToString() : value;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
