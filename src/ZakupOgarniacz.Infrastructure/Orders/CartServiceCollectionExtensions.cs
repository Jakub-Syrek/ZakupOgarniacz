using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Infrastructure.Orders;

// W namespace Microsoft.Extensions.DependencyInjection, żeby metoda była dostępna
// w Program.cs bez dodatkowego usinga.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja magazynu koszyków.</summary>
public static class CartServiceCollectionExtensions
{
    /// <summary>Rejestruje <see cref="ICartStore"/> w wariancie in-memory (do wymiany na EF Core).</summary>
    public static IServiceCollection AddInMemoryCartStore(this IServiceCollection services)
    {
        services.AddSingleton<ICartStore, InMemoryCartStore>();
        return services;
    }
}
