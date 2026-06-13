namespace ZakupOgarniacz.Providers.OpenFoodFacts;

/// <summary>
/// Konfiguracja adaptera Open Food Facts (wiązana z sekcją <c>OpenFoodFacts</c> w appsettings).
/// </summary>
public sealed class OpenFoodFactsOptions
{
    public const string SectionName = "OpenFoodFacts";

    /// <summary>Bazowy adres API. Musi kończyć się ukośnikiem.</summary>
    public string BaseUrl { get; set; } = "https://world.openfoodfacts.org/";

    /// <summary>
    /// Nagłówek User-Agent. Open Food Facts prosi o identyfikację aplikacji
    /// (najlepiej z kontaktem) — patrz https://world.openfoodfacts.org/data.
    /// </summary>
    public string UserAgent { get; set; } = "ZakupOgarniacz/0.1";
}
