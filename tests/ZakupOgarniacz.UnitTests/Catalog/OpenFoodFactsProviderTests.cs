using System.Net;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Providers.OpenFoodFacts;
using ZakupOgarniacz.UnitTests.TestDoubles;

namespace ZakupOgarniacz.UnitTests.Catalog;

public class OpenFoodFactsProviderTests
{
    private const string SearchJson = """
        {
          "count": 2,
          "products": [
            { "code": "111", "product_name": "Mleko 2%", "brands": "Łaciate", "image_url": "https://img/111.jpg", "nutriscore_grade": "b" },
            { "code": "222", "product_name": "Mleko 3.2%", "brands": "Mlekovita", "nutriscore_grade": "c" }
          ]
        }
        """;

    private const string ProductJson = """
        {
          "status": 1,
          "product": {
            "code": "111",
            "product_name": "Mleko 2%",
            "brands": "Łaciate",
            "quantity": "1 l",
            "image_front_url": "https://img/111-front.jpg",
            "categories": "Dairies, Milks, Cow's milk",
            "ingredients_text": "mleko",
            "allergens_tags": ["en:milk"],
            "nutriscore_grade": "b",
            "nutriments": {
              "energy-kcal_100g": 50,
              "fat_100g": 2.0,
              "saturated-fat_100g": "1.3",
              "carbohydrates_100g": 4.8,
              "sugars_100g": 4.8,
              "proteins_100g": 3.2,
              "salt_100g": 0.1
            }
          }
        }
        """;

    private static OpenFoodFactsProvider CreateProvider(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://world.openfoodfacts.org/") });

    [Fact]
    public async Task SearchAsync_mapuje_wyniki_i_paginacje()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(SearchJson));

        var result = await provider.SearchAsync("mleko", page: 2, pageSize: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Collection(
            result.Items,
            first =>
            {
                Assert.Equal("111", first.Code);
                Assert.Equal("Mleko 2%", first.Name);
                Assert.Equal("Łaciate", first.Brand);
                Assert.Equal("https://img/111.jpg", first.ImageUrl);
                Assert.Equal(NutriScore.B, first.NutriScore);
            },
            second =>
            {
                Assert.Equal("222", second.Code);
                Assert.Equal(NutriScore.C, second.NutriScore);
                Assert.Null(second.ImageUrl);
            });
    }

    [Fact]
    public async Task SearchAsync_buduje_url_z_fraza_i_paginacja()
    {
        var handler = new StubHttpMessageHandler(SearchJson);
        var provider = CreateProvider(handler);

        await provider.SearchAsync("mleko bez laktozy", page: 3, pageSize: 25);

        var url = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("cgi/search.pl", url);
        Assert.Contains("search_terms=mleko%20bez%20laktozy", url);
        Assert.Contains("page=3", url);
        Assert.Contains("page_size=25", url);
    }

    [Fact]
    public async Task GetProductAsync_mapuje_pelny_produkt()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(ProductJson));

        var product = await provider.GetProductAsync("111");

        Assert.NotNull(product);
        Assert.Equal("111", product!.Code);
        Assert.Equal("Mleko 2%", product.Name);
        Assert.Equal("1 l", product.Quantity);
        Assert.Equal("https://img/111-front.jpg", product.ImageUrl);
        Assert.Equal(NutriScore.B, product.NutriScore);
        Assert.Equal(["Dairies", "Milks", "Cow's milk"], product.Categories);
        Assert.Equal(["milk"], product.Allergens);

        Assert.Equal(50d, product.Nutrition.EnergyKcalPer100g);
        Assert.Equal(2.0d, product.Nutrition.FatPer100g);
        Assert.Equal(1.3d, product.Nutrition.SaturatedFatPer100g); // wartość przyszła jako string
        Assert.Equal(3.2d, product.Nutrition.ProteinsPer100g);
        Assert.Null(product.Nutrition.FiberPer100g);   // brak w źródle
        Assert.Null(product.Nutrition.SodiumPer100g);  // brak w źródle
    }

    [Fact]
    public async Task GetProductAsync_zwraca_null_gdy_status_zero()
    {
        var provider = CreateProvider(new StubHttpMessageHandler("""{ "status": 0, "product": null }"""));

        var product = await provider.GetProductAsync("000");

        Assert.Null(product);
    }

    [Fact]
    public async Task GetProductAsync_zwraca_null_dla_404()
    {
        var provider = CreateProvider(new StubHttpMessageHandler("", HttpStatusCode.NotFound));

        var product = await provider.GetProductAsync("000");

        Assert.Null(product);
    }

    [Fact]
    public async Task GetNutritionAsync_zwraca_wartosci_odzywcze()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(ProductJson));

        var nutrition = await provider.GetNutritionAsync("111");

        Assert.NotNull(nutrition);
        Assert.Equal(50d, nutrition!.EnergyKcalPer100g);
        Assert.Equal(4.8d, nutrition.SugarsPer100g);
    }
}
