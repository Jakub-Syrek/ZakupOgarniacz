using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Providers.Frisco;

// W namespace Microsoft.Extensions.DependencyInjection, żeby metoda była dostępna
// w Program.cs bez dodatkowego usinga (Web SDK importuje ten namespace).
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja sklepu Frisco.pl (katalog + zamawianie) w kontenerze DI.</summary>
public static class FriscoServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje <see cref="ICatalogProvider"/> (Frisco) jako typed <see cref="HttpClient"/>
    /// ze standardowym handlerem resilience (retry, circuit breaker, timeout) oraz
    /// <see cref="IOrderProvider"/>. Frisco nie ma Cloudflare — zwykły HTTP wystarcza.
    /// </summary>
    public static IServiceCollection AddFriscoStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FriscoOptions>(configuration.GetSection(FriscoOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FriscoOptions>>().Value);

        services.AddHttpClient<ICatalogProvider, FriscoProvider>(static (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<FriscoOptions>();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<IOrderProvider, FriscoOrderProvider>();

        return services;
    }

    /// <summary>
    /// Rejestruje zalogowanego klienta Frisco (<see cref="FriscoCheckoutClient"/>) do auto-checkoutu.
    /// Token i userId pochodzą z konfiguracji (sekcja <c>FriscoCheckout</c>; trzymaj je w
    /// user-secrets / zmiennych środowiskowych, nie w wersjonowanym appsettings).
    /// </summary>
    public static IServiceCollection AddFriscoCheckout(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FriscoCheckoutOptions>(configuration.GetSection(FriscoCheckoutOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FriscoCheckoutOptions>>().Value);

        services.AddHttpClient<FriscoCheckoutClient>(static (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<FriscoCheckoutOptions>();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ZakupOgarniacz/0.1)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (options.IsConfigured)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(options.AuthScheme, options.AccessToken);
            }
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
