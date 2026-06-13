using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ZakupOgarniacz.IntegrationTests;

public class CatalogEndpointsTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;

    public CatalogEndpointsTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_zwraca_200_z_wynikami()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/products/search?q=mleko");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());

        var first = body.GetProperty("items")[0];
        Assert.Equal("Mleko 2%", first.GetProperty("name").GetString());
        Assert.Equal("B", first.GetProperty("nutriScore").GetString()); // enum jako string
    }

    [Fact]
    public async Task Search_bez_frazy_zwraca_400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/products/search?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_istniejacy_zwraca_200_z_danymi()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/products/111");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mleko 2%", body.GetProperty("name").GetString());
        Assert.Equal("B", body.GetProperty("nutriScore").GetString());
        Assert.Equal(50d, body.GetProperty("nutrition").GetProperty("energyKcalPer100g").GetDouble());
    }

    [Fact]
    public async Task GetProduct_nieistniejacy_zwraca_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNutrition_istniejacy_zwraca_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/products/111/nutrition");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50d, body.GetProperty("energyKcalPer100g").GetDouble());
    }
}
