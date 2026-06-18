using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Providers.Carrefour;

namespace ZakupOgarniacz.UnitTests.Orders;

public class CarrefourOrderProviderTests
{
    private static Product Product(string id) => new(
        Id: id, Name: $"Produkt {id}", Brand: null, Ean: null, Sku: null, Size: null,
        ImageUrl: null, ProductUrl: $"https://www.carrefour.pl/produkt/{id}",
        Categories: [], Price: null, Available: true);

    [Fact]
    public async Task ExportCartAsync_buduje_liste_z_linkami()
    {
        var cart = new Cart();
        cart.Add(Product("111"), 2);
        cart.Add(Product("222"), 1);

        var export = await new CarrefourOrderProvider().ExportCartAsync(cart);

        Assert.Equal(2, export.Lines.Count);
        Assert.Collection(
            export.Lines,
            line =>
            {
                Assert.Equal("Produkt 111", line.ProductName);
                Assert.Equal(2, line.Quantity);
                Assert.Equal("https://www.carrefour.pl/produkt/111", line.ProductUrl);
            },
            line =>
            {
                Assert.Equal("Produkt 222", line.ProductName);
                Assert.Equal(1, line.Quantity);
            });
    }
}
