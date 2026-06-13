using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Infrastructure.Carrefour;
using ZakupOgarniacz.Providers.Carrefour;

// W namespace Microsoft.Extensions.DependencyInjection, żeby metoda była dostępna
// w Program.cs bez dodatkowego usinga (Web SDK importuje ten namespace).
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja sklepu Carrefour (katalog + zamawianie) w kontenerze DI.</summary>
public static class CarrefourServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje <see cref="ICatalogProvider"/> i <see cref="IOrderProvider"/> oparte o Carrefour,
    /// z transportem przez Playwright (wymóg Cloudflare).
    /// </summary>
    public static IServiceCollection AddCarrefourStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CarrefourOptions>(configuration.GetSection(CarrefourOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CarrefourOptions>>().Value);

        services.AddSingleton<ICarrefourTransport, PlaywrightCarrefourTransport>();
        services.AddSingleton<ICatalogProvider, CarrefourProvider>();
        services.AddSingleton<IOrderProvider, CarrefourOrderProvider>();

        return services;
    }
}
