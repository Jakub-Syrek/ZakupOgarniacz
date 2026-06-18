using Microsoft.EntityFrameworkCore;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Infrastructure.Orders;

// W namespace Microsoft.Extensions.DependencyInjection, żeby metoda była dostępna
// w Program.cs bez dodatkowego usinga.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja magazynu koszyków.</summary>
public static class CartServiceCollectionExtensions
{
    /// <summary>Rejestruje <see cref="ICartStore"/> w wariancie in-memory (np. do testów).</summary>
    public static IServiceCollection AddInMemoryCartStore(this IServiceCollection services)
    {
        services.AddSingleton<ICartStore, InMemoryCartStore>();
        return services;
    }

    /// <summary>Rejestruje <see cref="ICartStore"/> na EF Core + SQLite (persystencja koszyka).</summary>
    public static IServiceCollection AddSqliteCartStore(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<CartDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<ICartStore, SqliteCartStore>();
        return services;
    }
}
