using Microsoft.Playwright;
using ZakupOgarniacz.Providers.Carrefour;

namespace ZakupOgarniacz.Infrastructure.Carrefour;

/// <summary>
/// Transport Carrefour oparty o Playwright. carrefour.pl stoi za Cloudflare, więc żądania
/// do <c>/web/…</c> wykonujemy <b>w kontekście realnej przeglądarki</b> (page.fetch),
/// reużywając trwałego profilu (przejście przez Cloudflare / ewentualne logowanie).
/// </summary>
/// <remarks>
/// Wymaga zainstalowanych przeglądarek Playwright (<c>playwright install chromium</c>) oraz
/// — przy <see cref="CarrefourOptions.Headless"/> = <c>false</c> — środowiska z ekranem.
/// Ścieżka niepokryta testami automatycznymi (zależna od przeglądarki i Cloudflare).
/// </remarks>
public sealed class PlaywrightCarrefourTransport : ICarrefourTransport, IAsyncDisposable
{
    private readonly CarrefourOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;

    public PlaywrightCarrefourTransport(CarrefourOptions options) => _options = options;

    public async Task<string> GetJsonAsync(string relativePathAndQuery, CancellationToken cancellationToken = default)
    {
        var page = await EnsurePageAsync(cancellationToken);

        var path = relativePathAndQuery.StartsWith('/') ? relativePathAndQuery : "/" + relativePathAndQuery;

        // Fetch wykonany w kontekście strony — leci z cookies i fingerprintem przeglądarki.
        return await page.EvaluateAsync<string>(
            """
            async (path) => {
                const res = await fetch(path, { credentials: 'include', headers: { accept: 'application/json' } });
                if (!res.ok) throw new Error('Carrefour ' + res.status + ' for ' + path);
                return await res.text();
            }
            """,
            path);
    }

    private async Task<IPage> EnsurePageAsync(CancellationToken cancellationToken)
    {
        if (_page is not null)
        {
            return _page;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is not null)
            {
                return _page;
            }

            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

            var userDataDir = string.IsNullOrWhiteSpace(_options.UserDataDir)
                ? Path.Combine(Path.GetTempPath(), "zakupogarniacz-carrefour-profile")
                : _options.UserDataDir;

            _context = await _playwright.Chromium.LaunchPersistentContextAsync(
                userDataDir,
                new BrowserTypeLaunchPersistentContextOptions { Headless = _options.Headless });

            var page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();

            // Wejście na stronę ustanawia origin + sesję (przejście przez Cloudflare).
            await page.GotoAsync(_options.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            _page = page;
            return page;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }

        _playwright?.Dispose();
        _gate.Dispose();
    }
}
