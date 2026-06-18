namespace ZakupOgarniacz.Core.Orders;

/// <summary>
/// Magazyn koszyków. Na start in-memory; docelowo persystencja (EF Core + SQLite).
/// </summary>
public interface ICartStore
{
    /// <summary>Tworzy nowy, pusty koszyk i zwraca go (z nadanym <see cref="Cart.Id"/>).</summary>
    Task<Cart> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>Pobiera koszyk po id; <c>null</c> gdy nie istnieje.</summary>
    Task<Cart?> GetAsync(string cartId, CancellationToken cancellationToken = default);

    /// <summary>Zapisuje stan koszyka.</summary>
    Task SaveAsync(Cart cart, CancellationToken cancellationToken = default);
}
