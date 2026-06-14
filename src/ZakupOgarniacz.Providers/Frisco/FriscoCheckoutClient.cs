namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Zalogowany klient Frisco do auto-checkoutu (do ekranu płatności). Używa tokena
/// z konfiguracji (<see cref="FriscoCheckoutOptions"/>) i user-scoped endpointów
/// <c>/users/{id}/cart…</c>. Granica: NIE wykonuje płatności.
/// </summary>
/// <remarks>
/// Payloady (<c>POST /cart/products</c>, rezerwacja dostawy, <c>cart/order</c>) zostaną
/// uzupełnione po przechwyceniu realnych żądań z DevTools — patrz metody z TODO.
/// </remarks>
public sealed class FriscoCheckoutClient
{
    private readonly HttpClient _http;
    private readonly FriscoCheckoutOptions _options;

    public FriscoCheckoutClient(HttpClient http, FriscoCheckoutOptions options)
    {
        _http = http;
        _options = options;
    }

    public bool IsConfigured => _options.IsConfigured;

    /// <summary>Pobiera surowy JSON koszyka użytkownika (weryfikacja, że token działa).</summary>
    public async Task<string> GetCartRawAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"users/{_options.UserId}/cart", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // TODO (po przechwyceniu payloadów z DevTools):
    // - AddProductAsync(productId, quantity)  -> POST users/{id}/cart/products
    // - GetDeliverySlotsAsync()               -> users/{id}/cart/saved-reservations
    // - ReserveSlotAsync(...)                 -> users/{id}/cart/reservation
    // - CreateOrderDraftAsync(...)            -> users/{id}/cart/order  (STOP przed płatnością/DotPay)
}
