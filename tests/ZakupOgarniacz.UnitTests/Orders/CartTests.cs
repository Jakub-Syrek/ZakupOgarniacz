using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;

namespace ZakupOgarniacz.UnitTests.Orders;

public class CartTests
{
    private static Product Product(string id, decimal? price) => new(
        Id: id,
        Name: $"Produkt {id}",
        Brand: null,
        Ean: null,
        Sku: null,
        Size: null,
        ImageUrl: null,
        ProductUrl: $"https://www.carrefour.pl/produkt/{id}",
        Categories: [],
        Price: price is { } p ? new Money(p) : null,
        Available: true);

    [Fact]
    public void Add_scala_ten_sam_produkt()
    {
        var cart = new Cart("c1");
        var milk = Product("111", 3.49m);

        cart.Add(milk, 2);
        cart.Add(milk, 3);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void Add_rozne_produkty_daje_osobne_pozycje()
    {
        var cart = new Cart();
        cart.Add(Product("111", 3.49m));
        cart.Add(Product("222", 5.00m));

        Assert.Equal(2, cart.Items.Count);
    }

    [Fact]
    public void Total_sumuje_gdy_wszystkie_pozycje_maja_cene()
    {
        var cart = new Cart();
        cart.Add(Product("111", 3.00m), 2); // 6.00
        cart.Add(Product("222", 5.00m), 1); // 5.00

        var total = cart.Total;

        Assert.NotNull(total);
        Assert.Equal(11.00m, total!.Amount);
        Assert.Equal("PLN", total.Currency);
    }

    [Fact]
    public void Total_jest_null_gdy_ktoras_pozycja_bez_ceny()
    {
        var cart = new Cart();
        cart.Add(Product("111", 3.00m));
        cart.Add(Product("222", null)); // cena nieznana (np. z wyszukiwania, bez sklepu)

        Assert.Null(cart.Total);
    }

    [Fact]
    public void Add_odrzuca_niedodatnia_ilosc()
    {
        var cart = new Cart();

        Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(Product("111", 1m), 0));
    }
}
