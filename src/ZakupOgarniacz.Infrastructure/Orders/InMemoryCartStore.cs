using System.Collections.Concurrent;
using ZakupOgarniacz.Core.Orders;

namespace ZakupOgarniacz.Infrastructure.Orders;

/// <summary>Prosty magazyn koszyków w pamięci procesu. Do wymiany na EF Core + SQLite.</summary>
public sealed class InMemoryCartStore : ICartStore
{
    private readonly ConcurrentDictionary<string, Cart> _carts = new();

    public Task<Cart> CreateAsync(CancellationToken cancellationToken = default)
    {
        var cart = new Cart();
        _carts[cart.Id] = cart;
        return Task.FromResult(cart);
    }

    public Task<Cart?> GetAsync(string cartId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.TryGetValue(cartId, out var cart) ? cart : null);

    public Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _carts[cart.Id] = cart;
        return Task.CompletedTask;
    }
}
