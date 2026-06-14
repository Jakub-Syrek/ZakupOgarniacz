using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>Wynik wywołania API Frisco (status + surowe ciało + użyty wariant) do relacji do UI.</summary>
public sealed record FriscoCartResult(int StatusCode, string Body, string? Route = null)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

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

    /// <summary>
    /// Wrzuca produkty do koszyka Frisco (batch <c>{ products: [{ productId, quantity }] }</c>).
    /// Zwraca status + ciało odpowiedzi (do relacji do UI). Granica: NIE płaci.
    /// </summary>
    // Kandydaci metoda+ścieżka dla dodawania do koszyka (auto-detekcja — recon dał niejednoznaczność).
    // 404/405 = zły route (nic nie zmienia) -> próbujemy następny; pierwszy inny wynik zwracamy.
    private static readonly (HttpMethod Method, string Suffix)[] AddCandidates =
    [
        (HttpMethod.Post, "cart"),
        (HttpMethod.Put, "cart"),
        (HttpMethod.Patch, "cart"),
        (HttpMethod.Post, "cart/products"),
        (HttpMethod.Put, "cart/products"),
    ];

    public async Task<FriscoCartResult> AddProductsAsync(
        IReadOnlyCollection<(string ProductId, int Quantity)> items,
        CancellationToken cancellationToken = default)
    {
        var creds = _credentials.Snapshot()
            ?? throw new InvalidOperationException("Brak skonfigurowanych poświadczeń Frisco.");
        var accessToken = await GetAccessTokenAsync(creds, cancellationToken);

        var payload = new
        {
            products = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray(),
        };

        FriscoCartResult? last = null;
        foreach (var (method, suffix) in AddCandidates)
        {
            var route = $"{method.Method} users/{creds.UserId}/{suffix}";
            using var request = new HttpRequestMessage(method, $"users/{creds.UserId}/{suffix}")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(creds.Scheme, accessToken);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            last = new FriscoCartResult(status, body, route);

            // 404/405 = ten wariant nie istnieje/nie przyjmuje metody — próbuj dalej (nic nie dodano).
            if (status is not (404 or 405))
            {
                return last;
            }
        }

        return last!;
    }

    /// <summary>
    /// Pobiera surowy JSON terminów dostawy + opcji płatności dla kodu pocztowego
    /// (<c>GET users/{id}/calendar/delivery-payment?postcode=…</c>). Tylko odczyt.
    /// </summary>
    public async Task<string> GetDeliveryPaymentRawAsync(string postcode, CancellationToken cancellationToken = default)
    {
        var creds = _credentials.Snapshot()
            ?? throw new InvalidOperationException("Brak skonfigurowanych poświadczeń Frisco.");
        var accessToken = await GetAccessTokenAsync(creds, cancellationToken);

        var path = $"users/{creds.UserId}/calendar/delivery-payment?postcode={Uri.EscapeDataString(postcode)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
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
