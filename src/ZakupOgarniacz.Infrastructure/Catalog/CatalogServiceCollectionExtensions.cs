using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Providers.OpenFoodFacts;

// Celowo w namespace Microsoft.Extensions.DependencyInjection, żeby metoda była
// dostępna w Program.cs bez dodatkowego usinga (Web SDK importuje ten namespace).
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja katalogu produktów (adapter Open Food Facts) w kontenerze DI.</summary>
public static class CatalogServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje <see cref="ICatalogProvider"/> jako typed <see cref="HttpClient"/>
    /// wskazujący na Open Food Facts, ze standardowym handlerem resilience
    /// (retry, circuit breaker, timeout — Polly via Microsoft.Extensions.Http.Resilience).
    /// </summary>
    public static IServiceCollection AddOpenFoodFactsCatalog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenFoodFactsOptions>(
            configuration.GetSection(OpenFoodFactsOptions.SectionName));

        services.AddHttpClient<ICatalogProvider, OpenFoodFactsProvider>(static (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenFoodFactsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
