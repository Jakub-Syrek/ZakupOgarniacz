using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Zalogowany klient Frisco do auto-checkoutu (do ekranu płatności). Sam dba o ważny
/// access-token: cache → odświeżenie przez OAuth2 <c>refresh_token</c> na
/// <see cref="FriscoCheckoutOptions.TokenEndpoint"/> (z rotacją refresh-tokena). Granica: NIE płaci.
/// </summary>
public sealed class FriscoCheckoutClient
{
    private readonly HttpClient _http;
    private readonly FriscoCredentialStore _credentials;
    private readonly FriscoCheckoutOptions _options;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public FriscoCheckoutClient(HttpClient http, FriscoCredentialStore credentials, FriscoCheckoutOptions options)
    {
        _http = http;
        _credentials = credentials;
        _options = options;
    }

    public bool IsConfigured => _credentials.IsConfigured;

    /// <summary>Pobiera surowy JSON koszyka użytkownika (weryfikacja, że auth działa).</summary>
    public async Task<string> GetCartRawAsync(CancellationToken cancellationToken = default)
    {
        var creds = _credentials.Snapshot()
            ?? throw new InvalidOperationException("Brak skonfigurowanych poświadczeń Frisco.");
        var accessToken = await GetAccessTokenAsync(creds, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{creds.UserId}/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue(creds.Scheme, accessToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(FriscoCredentials creds, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_credentials.GetCachedAccessToken(now) is { } cached)
        {
            return cached;
        }

        // Tryb direct (ręczny access-token, bez refresh): zwracamy jak jest (może wygasnąć -> 401).
        if (creds.RefreshToken is null)
        {
            return creds.DirectAccessToken
                ?? throw new InvalidOperationException("Brak access-tokena i refresh-tokena Frisco.");
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Drugi check pod blokadą (inny wątek mógł właśnie odświeżyć).
            if (_credentials.GetCachedAccessToken(DateTimeOffset.UtcNow) is { } fresh)
            {
                return fresh;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = creds.RefreshToken,
                ["client_id"] = creds.ClientId,
            });

            using var response = await _http.PostAsync(_options.TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Pusta odpowiedź z /connect/token.");
            if (string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException("Brak access_token w odpowiedzi /connect/token.");
            }

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn > 0 ? token.ExpiresIn : 300);
            _credentials.SetCachedAccessToken(token.AccessToken, expiresAt);
            if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                _credentials.UpdateRefreshToken(token.RefreshToken); // rotacja
            }

            return token.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    // TODO (po przechwyceniu payloadów): AddProductAsync -> POST users/{id}/cart/products,
    // dostawa (reservation/saved-reservations), draft zamówienia (cart/order) — STOP przed płatnością.
}
