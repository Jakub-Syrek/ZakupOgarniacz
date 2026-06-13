using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Providers.Carrefour;
using ZakupOgarniacz.UnitTests.TestDoubles;

namespace ZakupOgarniacz.UnitTests.Carrefour;

public class CarrefourProviderTests
{
    // Wierny kształtowi z reconu /web/catalog (nazwy pól zgodne z realnym API).
    private const string CatalogJson = """
        {
          "totalCount": 42,
          "totalPages": 3,
          "content": [
            {
              "id": "p-111",
              "name": "Mleko UHT 2% 1 l",
              "displayName": "Mleko UHT 2% Łaciate 1 l",
              "brandName": "Łaciate",
              "actualSku": "SKU111",
              "slug": "/produkt/mleko-uht-2-laciate-1l",
              "url": "/produkt/mleko-uht-2-laciate-1l",
              "active": true,
              "defaultCategoryName": "Mleko",
              "productCategories": [ { "name": "Nabiał" }, { "name": "Mleko" } ],
              "defaultImage": "/img/111.jpg",
              "images": [ "/img/111-a.jpg" ],
              "product": {
                "code": "5900512300108",
                "sizeWithUnitString": "1 l",
                "sellUnitString": "szt.",
                "grammageUnit": "l"
              }
            },
            {
              "id": "p-222",
              "name": "Mleko 3,2% Mlekovita",
              "brandName": "Mlekovita",
              "actualSku": "SKU222",
              "url": "/produkt/mleko-32-mlekovita",
              "active": false,
              "product": { "code": "5900512300222", "sizeWithUnitString": "1 l" }
            }
          ]
        }
        """;

    private static CarrefourProvider CreateProvider(string json, out FakeCarrefourTransport transport)
    {
        transport = new FakeCarrefourTransport(json);
        return new CarrefourProvider(transport, new CarrefourOptions { BaseUrl = "https://www.carrefour.pl/" });
    }

    [Fact]
    public async Task SearchAsync_mapuje_produkty_i_paginacje()
    {
        var provider = CreateProvider(CatalogJson, out _);

        var result = await provider.SearchAsync("mleko", page: 1, pageSize: 20);

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        var first = result.Items[0];
        Assert.Equal("p-111", first.Id);
        Assert.Equal("Mleko UHT 2% Łaciate 1 l", first.Name); // displayName ma pierwszeństwo
        Assert.Equal("Łaciate", first.Brand);
        Assert.Equal("5900512300108", first.Ean);
        Assert.Equal("SKU111", first.Sku);
        Assert.Equal("1 l", first.Size);
        Assert.Equal("https://www.carrefour.pl/produkt/mleko-uht-2-laciate-1l", first.ProductUrl);
        Assert.Equal("https://www.carrefour.pl/img/111.jpg", first.ImageUrl);
        Assert.Equal(["Nabiał", "Mleko"], first.Categories);
        Assert.True(first.Available);
        Assert.Null(first.Price); // cena niedostępna w wyszukiwaniu

        var second = result.Items[1];
        Assert.Equal("Mleko 3,2% Mlekovita", second.Name); // brak displayName -> name
        Assert.False(second.Available);
        Assert.Empty(second.Categories); // brak kategorii i defaultCategoryName
    }

    [Fact]
    public async Task SearchAsync_buduje_sciezke_z_fraza_size_i_strona()
    {
        var provider = CreateProvider(CatalogJson, out var transport);

        await provider.SearchAsync("mleko bez laktozy", page: 3, pageSize: 25);

        Assert.NotNull(transport.LastPath);
        Assert.Contains("web/catalog", transport.LastPath);
        Assert.Contains("search=mleko%20bez%20laktozy", transport.LastPath);
        Assert.Contains("size=25", transport.LastPath);
        Assert.Contains("page=2", transport.LastPath); // 0-based
    }

    [Fact]
    public async Task GetProductAsync_dopasowuje_po_kodzie_ean()
    {
        var provider = CreateProvider(CatalogJson, out _);

        var product = await provider.GetProductAsync("5900512300222");

        Assert.NotNull(product);
        Assert.Equal("p-222", product!.Id);
        Assert.Equal("5900512300222", product.Ean);
    }

    [Fact]
    public async Task SearchAsync_pusta_odpowiedz_daje_zero_wynikow()
    {
        var provider = CreateProvider("""{ "content": [], "totalCount": 0 }""", out _);

        var result = await provider.SearchAsync("xyz");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
