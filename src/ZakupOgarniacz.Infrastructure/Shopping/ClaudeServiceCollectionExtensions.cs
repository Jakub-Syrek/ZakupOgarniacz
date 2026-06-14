using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZakupOgarniacz.Core.Shopping;
using ZakupOgarniacz.Infrastructure.Shopping;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Rejestracja parsera poleceń zakupowych opartego o Claude.</summary>
public static class ClaudeServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje <see cref="IShoppingListParser"/> (Claude). Klucz API bierze z sekcji
    /// <c>Claude:ApiKey</c>, a w razie braku — ze zmiennej <c>ANTHROPIC_API_KEY</c>.
    /// </summary>
    public static IServiceCollection AddClaudeShoppingParser(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ClaudeOptions>(options =>
        {
            configuration.GetSection(ClaudeOptions.SectionName).Bind(options);
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = configuration["ANTHROPIC_API_KEY"]
                    ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            }
        });
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClaudeOptions>>().Value);
        services.AddSingleton<IShoppingListParser, ClaudeShoppingListParser>();

        return services;
    }
}
