namespace ZakupOgarniacz.Infrastructure.Frisco;

/// <summary>
/// Konfiguracja auto-loginu do Frisco przez Playwright. Sekcja <c>FriscoCheckout:AutoLogin</c>.
/// Domyślnie tryb interaktywny (okno + ręczne logowanie usera) — nie wymaga hasła w apce.
/// Tryb bezobsługowy (login+hasło) jest opcjonalny: poświadczenia tylko z env/user-secrets,
/// NIGDY nie wersjonowane.
/// </summary>
public sealed class FriscoAutoLoginOptions
{
    public const string SectionName = "FriscoCheckout:AutoLogin";

    /// <summary>Strona logowania Frisco (otwierana w przeglądarce automatu).</summary>
    public string LoginUrl { get; set; } = "https://www.frisco.pl/login";

    /// <summary>
    /// Headless dla trybu bezobsługowego (login+hasło). Tryb interaktywny zawsze otwiera
    /// widoczne okno (inaczej nie da się zalogować ręcznie).
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Katalog trwałego profilu przeglądarki — sesja Frisco przeżywa restart procesu,
    /// więc kolejne uruchomienia łapią token bez ponownego logowania.
    /// </summary>
    public string? UserDataDir { get; set; }

    /// <summary>Limit czasu na przechwycenie tokenu (ms). Interaktywnie daj czas na zalogowanie.</summary>
    public int TimeoutMs { get; set; } = 180_000;

    /// <summary>Selektory pola loginu (tryb bezobsługowy) — próbowane po kolei.</summary>
    public string[] UsernameSelectors { get; set; } =
        ["input[type=email]", "input[name=username]", "input[name=email]", "input[name=login]", "#username", "#login", "#email"];

    /// <summary>Selektory pola hasła (tryb bezobsługowy) — próbowane po kolei.</summary>
    public string[] PasswordSelectors { get; set; } =
        ["input[type=password]", "input[name=password]", "#password"];

    /// <summary>Selektory przycisku „Zaloguj" (tryb bezobsługowy) — próbowane po kolei.</summary>
    public string[] SubmitSelectors { get; set; } =
        ["button[type=submit]", "button:has-text('Zaloguj')", "input[type=submit]"];

    /// <summary>Login do trybu bezobsługowego (env-fallback, jeśli nie podany w żądaniu). NIE wersjonować.</summary>
    public string? Username { get; set; }

    /// <summary>Hasło do trybu bezobsługowego (env-fallback). Tylko w pamięci/env. NIE wersjonować.</summary>
    public string? Password { get; set; }
}
