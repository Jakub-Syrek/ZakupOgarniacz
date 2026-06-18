using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Core.Orders;

/// <summary>Pozycja koszyka: produkt + ilość. Wartość pozycji znana tylko, gdy produkt ma cenę.</summary>
public sealed record CartItem(Product Product, int Quantity)
{
    public Money? LineTotal => Product.Price is { } price ? price.Multiply(Quantity) : null;
}
