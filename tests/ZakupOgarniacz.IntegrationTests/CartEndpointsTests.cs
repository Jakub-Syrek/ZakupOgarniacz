using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ZakupOgarniacz.IntegrationTests;

public class CartEndpointsTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;

    public CartEndpointsTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> CreateCartAsync(HttpClient client)
    {
        var response = await client.PostAsync("/carts", content: null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Pelny_przeplyw_dodaj_pokaz_eksportuj()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateCartAsync(client);

        // dodanie 2 szt. produktu 111 (atrapa: cena 6,99)
        var add = await client.PostAsJsonAsync($"/carts/{cartId}/items", new { code = "111", quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        // podgląd koszyka: 1 pozycja, ilość 2, suma 13,98
        var getResponse = await client.GetAsync($"/carts/{cartId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var cart = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = cart.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(2, items[0].GetProperty("quantity").GetInt32());
        Assert.Equal(13.98m, cart.GetProperty("total").GetProperty("amount").GetDecimal());

        // eksport: lista z nazwą i ilością
        var export = await client.PostAsync($"/carts/{cartId}/export", content: null);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var exportBody = await export.Content.ReadFromJsonAsync<JsonElement>();
        var line = exportBody.GetProperty("lines")[0];
        Assert.Equal("Mleko UHT 2% Łaciate 1 l", line.GetProperty("productName").GetString());
        Assert.Equal(2, line.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Clear_oprozni_koszyk()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateCartAsync(client);
        await client.PostAsJsonAsync($"/carts/{cartId}/items", new { code = "111", quantity = 2 });

        var clear = await client.PostAsync($"/carts/{cartId}/clear", content: null);
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        var cart = await clear.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());

        var get = await client.GetAsync($"/carts/{cartId}");
        var body = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Dodanie_nieznanego_produktu_zwraca_400()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateCartAsync(client);

        var add = await client.PostAsJsonAsync($"/carts/{cartId}/items", new { code = "999", quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }

    [Fact]
    public async Task Get_nieistniejacego_koszyka_zwraca_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/carts/nieistnieje");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FromCommand_buduje_koszyk_z_polecenia()
    {
        var client = _factory.CreateClient();

        // atrapa parsera zwraca [("mleko", 2)]; atrapa katalogu mapuje to na produkt 111 (6,99)
        var response = await client.PostAsJsonAsync("/carts/from-command", new { command = "kup mi mleko" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var items = body.GetProperty("cart").GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(2, items[0].GetProperty("quantity").GetInt32());
        Assert.Equal(13.98m, body.GetProperty("cart").GetProperty("total").GetProperty("amount").GetDecimal());

        Assert.Equal("Mleko UHT 2% Łaciate 1 l", body.GetProperty("report")[0].GetProperty("matched").GetString());
    }
}
