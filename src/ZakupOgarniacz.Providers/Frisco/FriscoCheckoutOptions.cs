namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Konfiguracja zalogowanego klienta Frisco (auto-checkout do ekranu płatności).
/// Endpointy są user-scoped (<c>/users/{UserId}/…</c>) i wymagają tokena z sesji
/// użytkownika — token wkleja użytkownik (np. user-secrets / zmienna środowiskowa),
/// NIE jest wersjonowany. Sekcja <c>FriscoCheckout</c>.
/// </summary>
public sealed class FriscoCheckoutOptions
{
    public const string SectionName = "FriscoCheckout";

    /// <summary>Baza API (commerce-proxy). Musi kończyć się ukośnikiem.</summary>
    public string BaseUrl { get; set; } = "https://www.frisco.pl/app/commerce/api/v1/";

    /// <summary>Id zalogowanego użytkownika Frisco (z URL-i <c>/users/{id}/…</c>).</summary>
    public string? UserId { get; set; }

    /// <summary>Token autoryzacji z sesji przeglądarki (wygasa — do odświeżania ręcznie).</summary>
    public string? AccessToken { get; set; }

    /// <summary>Schemat nagłówka Authorization (do potwierdzenia z przechwyconego żądania).</summary>
    public string AuthScheme { get; set; } = "Bearer";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(AccessToken);
}
