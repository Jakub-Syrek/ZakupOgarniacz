namespace ZakupOgarniacz.Providers.Carrefour;

/// <summary>Konfiguracja adaptera Carrefour (sekcja <c>Carrefour</c> w appsettings).</summary>
public sealed class CarrefourOptions
{
    public const string SectionName = "Carrefour";

    /// <summary>Bazowy adres sklepu. Musi kończyć się ukośnikiem.</summary>
    public string BaseUrl { get; set; } = "https://www.carrefour.pl/";

    /// <summary>Domyślny rozmiar strony wyników.</summary>
    public int DefaultPageSize { get; set; } = 20;

    // Ustawienia transportu przeglądarkowego (używane przez Infrastructure / Playwright):

    /// <summary>
    /// Katalog trwałego profilu przeglądarki. Reużycie profilu (z przejściem przez Cloudflare,
    /// ewentualnie zalogowaniem) jest kluczowe — świeży, automatyczny browser bywa blokowany.
    /// </summary>
    public string? UserDataDir { get; set; }

    /// <summary>Tryb bezokienkowy. Cloudflare bywa „łaskawszy" dla trybu z oknem (false).</summary>
    public bool Headless { get; set; }
}
