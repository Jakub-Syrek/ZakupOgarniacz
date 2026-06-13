using ZakupOgarniacz.Core.Catalog;

namespace ZakupOgarniacz.Core.Orders;

/// <summary>
/// Koszyk budowany po naszej stronie (nie w sklepie). Pozycje o tym samym produkcie
/// są scalane. <see cref="Total"/> jest znane tylko, gdy wszystkie pozycje mają cenę.
/// </summary>
public sealed class Cart
{
    private readonly List<CartItem> _items = [];

    public Cart(string? id = null) => Id = id ?? Guid.NewGuid().ToString("n");

    public string Id { get; }

    public IReadOnlyList<CartItem> Items => _items;

    /// <summary>Dodaje produkt (scala ilość, jeśli produkt już jest w koszyku).</summary>
    public void Add(Product product, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var index = _items.FindIndex(i => i.Product.Id == product.Id);
        if (index >= 0)
        {
            _items[index] = _items[index] with { Quantity = _items[index].Quantity + quantity };
        }
        else
        {
            _items.Add(new CartItem(product, quantity));
        }
    }

    /// <summary>Suma wartości koszyka — <c>null</c>, jeśli któraś pozycja nie ma ceny.</summary>
    public Money? Total
    {
        get
        {
            if (_items.Count == 0 || _items.Any(i => i.Product.Price is null))
            {
                return null;
            }

            var currency = _items[0].Product.Price!.Currency;
            var sum = _items.Sum(i => i.Product.Price!.Amount * i.Quantity);
            return new Money(sum, currency);
        }
    }
}
