namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>Konfiguracja adaptera Frisco (sekcja <c>Frisco</c> w appsettings).</summary>
public sealed class FriscoOptions
{
    public const string SectionName = "Frisco";

    /// <summary>Bazowy adres API (commerce-proxy). Musi kończyć się ukośnikiem.</summary>
    public string BaseUrl { get; set; } = "https://www.frisco.pl/app/commerce/api/v1/";

    /// <summary>User-Agent. Frisco oddaje JSON na przeglądarkowy UA — trzymamy realistyczny.</summary>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36";

    /// <summary>Domyślny rozmiar strony.</summary>
    public int DefaultPageSize { get; set; } = 25;
}
