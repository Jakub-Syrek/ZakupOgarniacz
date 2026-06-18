namespace ZakupOgarniacz.Providers.Carrefour;

/// <summary>
/// Transport do wewnętrznego API Carrefour (ścieżki <c>/web/…</c>). Implementacja musi
/// wykonać żądanie w kontekście sesji przeglądarki — carrefour.pl stoi za Cloudflare,
/// więc zwykły serwerowy HTTP dostaje 403 (patrz README → „Realia integracji").
/// Konkretną implementację (Playwright) dostarcza warstwa Infrastructure.
/// </summary>
public interface ICarrefourTransport
{
    /// <summary>
    /// Wykonuje GET na ścieżce względnej (np. <c>web/catalog?search=mleko&amp;size=20</c>)
    /// w obrębie sesji przeglądarki i zwraca surowe ciało odpowiedzi (JSON).
    /// </summary>
    Task<string> GetJsonAsync(string relativePathAndQuery, CancellationToken cancellationToken = default);
}
