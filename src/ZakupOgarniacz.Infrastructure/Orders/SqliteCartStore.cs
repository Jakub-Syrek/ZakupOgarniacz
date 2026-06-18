using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;

namespace ZakupOgarniacz.Infrastructure.Orders;

/// <summary>
/// Magazyn koszyków na EF Core + SQLite. Koszyk serializowany jest do JSON-a i trzymany
/// jako pojedynczy dokument (wzorzec agregatu) — patrz <see cref="CartDbContext"/>.
/// </summary>
public sealed class SqliteCartStore : ICartStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDbContextFactory<CartDbContext> _factory;

    public SqliteCartStore(IDbContextFactory<CartDbContext> factory) => _factory = factory;

    public async Task<Cart> CreateAsync(CancellationToken cancellationToken = default)
    {
        var cart = new Cart();
        await SaveAsync(cart, cancellationToken);
        return cart;
    }

    public async Task<Cart?> GetAsync(string cartId, CancellationToken cancellationToken = default)
    {
        await using var db = await OpenAsync(cancellationToken);

        var document = await db.Carts.FindAsync([cartId], cancellationToken);
        if (document is null)
        {
            return null;
        }

        var snapshot = JsonSerializer.Deserialize<CartSnapshot>(document.Json, JsonOptions);
        return snapshot is null ? null : ToCart(snapshot);
    }

    public async Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await using var db = await OpenAsync(cancellationToken);

        var json = JsonSerializer.Serialize(ToSnapshot(cart), JsonOptions);

        var document = await db.Carts.FindAsync([cart.Id], cancellationToken);
        if (document is null)
        {
            db.Carts.Add(new CartDocument { Id = cart.Id, Json = json });
        }
        else
        {
            document.Json = json;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CartDbContext> OpenAsync(CancellationToken cancellationToken)
    {
        var db = await _factory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        return db;
    }

    private static CartSnapshot ToSnapshot(Cart cart) => new(
        cart.Id,
        cart.Items.Select(i => new CartItemSnapshot(i.Product, i.Quantity)).ToList());

    private static Cart ToCart(CartSnapshot snapshot)
    {
        var cart = new Cart(snapshot.Id);
        foreach (var item in snapshot.Items)
        {
            cart.Add(item.Product, item.Quantity);
        }

        return cart;
    }

    // Postać serializowana koszyka (snapshot agregatu).
    private sealed record CartSnapshot(string Id, List<CartItemSnapshot> Items);

    private sealed record CartItemSnapshot(Product Product, int Quantity);
}
