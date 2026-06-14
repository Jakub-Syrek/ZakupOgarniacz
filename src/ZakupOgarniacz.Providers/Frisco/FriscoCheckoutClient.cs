using System.Net.Http.Headers;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Zalogowany klient Frisco do auto-checkoutu (do ekranu płatności). Token bierze z
/// <see cref="FriscoCredentialStore"/> (edytowalny w locie) i ustawia nagłówek autoryzacji
/// per żądanie. Granica: NIE wykonuje płatności.
/// </summary>
/// <remarks>
/// Payloady (<c>POST /cart/products</c>, rezerwacja dostawy, <c>cart/order</c>) zostaną
/// uzupełnione po przechwyceniu realnych żądań z DevTools — patrz TODO.
/// </remarks>
public sealed class FriscoCheckoutClient
{
    private readonly HttpClient _http;
    private readonly FriscoCredentialStore _credentials;

    public FriscoCheckoutClient(HttpClient http, FriscoCredentialStore credentials)
    {
        _http = http;
        _credentials = credentials;
    }

    public bool IsConfigured => _credentials.IsConfigured;

    /// <summary>Pobiera surowy JSON koszyka użytkownika (weryfikacja, że token działa).</summary>
    public async Task<string> GetCartRawAsync(CancellationToken cancellationToken = default)
    {
        var creds = _credentials.Snapshot()
            ?? throw new InvalidOperationException("Brak skonfigurowanego tokena Frisco.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{creds.UserId}/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue(creds.Scheme, creds.Token);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // TODO (po przechwyceniu payloadów z DevTools):
    // - AddProductAsync(productId, quantity)  -> POST users/{id}/cart/products
    // - GetDeliverySlotsAsync()               -> users/{id}/cart/saved-reservations
    // - ReserveSlotAsync(...)                 -> users/{id}/cart/reservation
    // - CreateOrderDraftAsync(...)            -> users/{id}/cart/order  (STOP przed płatnością/DotPay)
}
