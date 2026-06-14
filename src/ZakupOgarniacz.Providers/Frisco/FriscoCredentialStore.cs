namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>Poświadczenia zalogowanej sesji Frisco (do auto-checkoutu).</summary>
public sealed record FriscoCredentials(string UserId, string Token, string Scheme);

/// <summary>
/// Mutowalny, wątkowo-bezpieczny magazyn tokena Frisco. Pozwala wkleić/odświeżyć token
/// w trakcie działania (token z sesji wygasa) bez restartu. Seedowany z konfiguracji.
/// Token trzymany tylko w pamięci procesu.
/// </summary>
public sealed class FriscoCredentialStore
{
    private readonly object _lock = new();
    private string? _userId;
    private string? _token;
    private string _scheme = "Bearer";

    public FriscoCredentialStore(FriscoCheckoutOptions seed)
    {
        _userId = seed.UserId;
        _token = seed.AccessToken;
        if (!string.IsNullOrWhiteSpace(seed.AuthScheme))
        {
            _scheme = seed.AuthScheme;
        }
    }

    public bool IsConfigured
    {
        get { lock (_lock) { return !string.IsNullOrWhiteSpace(_userId) && !string.IsNullOrWhiteSpace(_token); } }
    }

    public void Set(string userId, string token, string? scheme)
    {
        lock (_lock)
        {
            _userId = userId;
            _token = token;
            _scheme = string.IsNullOrWhiteSpace(scheme) ? "Bearer" : scheme;
        }
    }

    /// <summary>Zwraca aktualne poświadczenia albo <c>null</c>, gdy nieustawione.</summary>
    public FriscoCredentials? Snapshot()
    {
        lock (_lock)
        {
            return IsConfigured ? new FriscoCredentials(_userId!, _token!, _scheme) : null;
        }
    }
}
