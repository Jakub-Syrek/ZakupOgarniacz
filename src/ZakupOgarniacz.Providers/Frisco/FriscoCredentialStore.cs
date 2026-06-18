namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>Migawka poświadczeń Frisco używana przez klienta.</summary>
public sealed record FriscoCredentials(
    string UserId,
    string Scheme,
    string? RefreshToken,
    string ClientId,
    string? DirectAccessToken);

/// <summary>
/// Mutowalny, wątkowo-bezpieczny magazyn poświadczeń Frisco. Dwa tryby:
/// <list type="bullet">
/// <item><b>refresh</b> (zalecany): trzyma refresh_token + client_id; access-token mintuje
/// i cache'uje klient (auto-odświeżanie, gdy wygaśnie).</item>
/// <item><b>direct</b>: ręcznie wklejony access-token (wygasa ~10 min).</item>
/// </list>
/// Wszystko tylko w pamięci procesu.
/// </summary>
public sealed class FriscoCredentialStore
{
    private readonly object _lock = new();
    private string? _userId;
    private string _scheme = "Bearer";
    private string _clientId;
    private string? _refreshToken;
    private string? _directAccessToken;
    private string? _cachedAccessToken;
    private DateTimeOffset _cachedExpiresAtUtc;

    public FriscoCredentialStore(FriscoCheckoutOptions seed)
    {
        _clientId = string.IsNullOrWhiteSpace(seed.ClientId) ? "frisco-web" : seed.ClientId;
        if (!string.IsNullOrWhiteSpace(seed.AuthScheme))
        {
            _scheme = seed.AuthScheme;
        }

        _userId = seed.UserId;
        _directAccessToken = seed.AccessToken;
    }

    public bool IsConfigured
    {
        get
        {
            lock (_lock)
            {
                return !string.IsNullOrWhiteSpace(_userId)
                    && (!string.IsNullOrWhiteSpace(_refreshToken) || !string.IsNullOrWhiteSpace(_directAccessToken));
            }
        }
    }

    /// <summary>Tryb refresh: refresh_token + (opcjonalnie) client_id. Czyści tryb direct i cache.</summary>
    public void SetRefreshToken(string userId, string refreshToken, string? clientId)
    {
        lock (_lock)
        {
            _userId = userId;
            _refreshToken = refreshToken;
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                _clientId = clientId;
            }

            _directAccessToken = null;
            _cachedAccessToken = null;
        }
    }

    /// <summary>Tryb direct: ręcznie wklejony access-token. Czyści tryb refresh i cache.</summary>
    public void SetAccessToken(string userId, string accessToken)
    {
        lock (_lock)
        {
            _userId = userId;
            _directAccessToken = accessToken;
            _refreshToken = null;
            _cachedAccessToken = null;
        }
    }

    public FriscoCredentials? Snapshot()
    {
        lock (_lock)
        {
            return IsConfigured
                ? new FriscoCredentials(_userId!, _scheme, _refreshToken, _clientId, _directAccessToken)
                : null;
        }
    }

    /// <summary>Zwraca cache'owany access-token, jeśli jeszcze ważny (z 30 s buforem), inaczej <c>null</c>.</summary>
    public string? GetCachedAccessToken(DateTimeOffset nowUtc)
    {
        lock (_lock)
        {
            if (_cachedAccessToken is not null && nowUtc < _cachedExpiresAtUtc.AddSeconds(-30))
            {
                return _cachedAccessToken;
            }

            return null;
        }
    }

    public void SetCachedAccessToken(string accessToken, DateTimeOffset expiresAtUtc)
    {
        lock (_lock)
        {
            _cachedAccessToken = accessToken;
            _cachedExpiresAtUtc = expiresAtUtc;
        }
    }

    /// <summary>Aktualizuje refresh_token po rotacji (OpenIddict rotuje przy każdym odświeżeniu).</summary>
    public void UpdateRefreshToken(string refreshToken)
    {
        lock (_lock)
        {
            _refreshToken = refreshToken;
        }
    }
}
