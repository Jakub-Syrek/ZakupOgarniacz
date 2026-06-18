using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Providers.Frisco;
using ZakupOgarniacz.UnitTests.TestDoubles;

namespace ZakupOgarniacz.UnitTests.Frisco;

public class FriscoProviderTests
{
    // Wierny zrzut z realnego /offer/products/query (skrócony do 2 produktów).
    private const string QueryJson = """
        {
          "pageIndex": 1,
          "pageSize": 25,
          "totalCount": 13489,
          "products": [
            {
              "productId": "99999",
              "product": {
                "id": "99999", "productId": "99999", "ean": "22229878",
                "unitOfMeasure": "Kilogram", "grammage": 0.600,
                "producer": "BUKAT", "brand": "FRISCO FRESH",
                "name": { "pl": "Pomidory malinowe 3-4szt.", "en": "Tomato" },
                "categories": [
                  { "depth": 1, "name": { "pl": "Pomidory", "en": "Tomatoes" } },
                  { "depth": 0, "name": { "pl": "Warzywa i owoce", "en": "Fruits and Vegetables" } }
                ],
                "isAvailable": true,
                "price": { "price": 6.99, "priceAfterPromotion": 4.79 },
                "imageUrl": "https://res.cloudinary.com/dj484tw6k/image/upload/v1/be/99999.jpg"
              }
            },
            {
              "productId": "3504",
              "product": {
                "productId": "3504", "ean": "1222308800017",
                "unitOfMeasure": "Kilogram", "grammage": 0.115,
                "brand": "FRISCO FRESH",
                "name": { "pl": "Rzodkiewka pęczek" },
                "isAvailable": false,
                "price": { "price": 3.19 }
              }
            }
          ]
        }
        """;

    private static FriscoProvider CreateProvider(string json, out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://www.frisco.pl/app/commerce/api/v1/") };
        return new FriscoProvider(http);
    }

    [Fact]
    public async Task SearchAsync_mapuje_produkty_cene_i_paginacje()
    {
        var provider = CreateProvider(QueryJson, out _);

        var result = await provider.SearchAsync("mleko", page: 1, pageSize: 25);

        Assert.Equal(13489, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal("99999", first.Id);
        Assert.Equal("Pomidory malinowe 3-4szt.", first.Name); // name.pl
        Assert.Equal("FRISCO FRESH", first.Brand);
        Assert.Equal("22229878", first.Ean);
        Assert.Equal("0.6 kg", first.Size);
        Assert.NotNull(first.Price);
        Assert.Equal(6.99m, first.Price!.Amount);
        Assert.Equal("PLN", first.Price.Currency);
        Assert.Equal(4.79m, first.PromotionalPrice!.Amount); // priceAfterPromotion
        Assert.True(first.Available);
        Assert.Equal("https://res.cloudinary.com/dj484tw6k/image/upload/v1/be/99999.jpg", first.ImageUrl);
        Assert.Equal(["Warzywa i owoce", "Pomidory"], first.Categories); // sortowane wg depth

        var second = result.Items[1];
        Assert.False(second.Available);
        Assert.Equal(3.19m, second.Price!.Amount);
        Assert.Null(second.PromotionalPrice); // brak promocji
        Assert.Empty(second.Categories);
    }

    [Fact]
    public async Task SearchAsync_buduje_url_z_query_i_paginacja()
    {
        var provider = CreateProvider(QueryJson, out var handler);

        await provider.SearchAsync("mleko bez laktozy", page: 3, pageSize: 10);

        var url = handler.LastRequestUri!.AbsoluteUri;
        Assert.Contains("offer/products/query", url);
        Assert.Contains("search=mleko%20bez%20laktozy", url);
        Assert.Contains("pageIndex=3", url);
        Assert.Contains("pageSize=10", url);
    }

    [Fact]
    public async Task GetProductAsync_dopasowuje_po_ean()
    {
        var provider = CreateProvider(QueryJson, out _);

        var product = await provider.GetProductAsync("1222308800017");

        Assert.NotNull(product);
        Assert.Equal("3504", product!.Id);
    }
}
