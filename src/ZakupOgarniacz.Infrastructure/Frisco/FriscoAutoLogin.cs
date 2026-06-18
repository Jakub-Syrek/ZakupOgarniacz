using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ZakupOgarniacz.Providers.Frisco;

namespace ZakupOgarniacz.Infrastructure.Frisco;

/// <summary>Auto-login się nie powiódł (nie przechwycono tokenu, brak pól formularza itp.).</summary>
public sealed class FriscoLoginException : Exception
{
    public FriscoLoginException(string message) : base(message)
    {
    }
}

/// <summary>
/// Frisco wymaga kroku, którego automat NIE wykonuje (captcha / 2FA). Dokończ ręcznie.
/// </summary>
public sealed class FriscoManualStepRequiredException : Exception
{
    public FriscoManualStepRequiredException(string message) : base(message)
    {
    }
}

/// <summary>Wynik auto-loginu. Token NIE jest zwracany do UI — trafia wprost do magazynu poświadczeń.</summary>
public sealed record FriscoLoginResult(string UserId, bool HasRefreshToken);

/// <summary>
/// Automatyzuje pozyskanie tokenu Frisco przez Playwright. Dwa tryby:
/// <list type="bullet">
/// <item><b>interaktywny</b> (domyślny, bez loginu/hasła): otwiera widoczne okno Frisco, user
/// loguje się sam (hasło wpisuje w prawdziwą stronę Frisco), automat tylko przechwytuje token.</item>
/// <item><b>bezobsługowy</b> (login+hasło z env/żądania): wypełnia formularz i wysyła.</item>
/// </list>
/// Token (refresh/access) czytany z odpowiedzi <c>/connect/token</c> (fallback: storage przeglądarki)
/// i zapisywany do <see cref="FriscoCredentialStore"/>. Profil jest trwały — sesja przeżywa restart.
/// Granica: hasło tylko w pamięci/env (nigdy logowane ani zapisywane), captcha/2FA NIE są obchodzone.
/// </summary>
public sealed partial class FriscoAutoLogin
{
    private readonly FriscoAutoLoginOptions _options;
    private readonly FriscoCredentialStore _store;
    private readonly ILogger<FriscoAutoLogin> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FriscoAutoLogin(FriscoAutoLoginOptions options, FriscoCredentialStore store, ILogger<FriscoAutoLogin> logger)
    {
        _options = options;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Loguje (interaktywnie lub bezobsługowo), przechwytuje token i zapisuje do magazynu.
    /// Brak <paramref name="username"/>/<paramref name="password"/> ⇒ tryb interaktywny.
    /// </summary>
    public async Task<FriscoLoginResult> CaptureAndStoreAsync(
        string? username,
        string? password,
        CancellationToken cancellationToken = default)
    {
        // Fallback na poświadczenia z env/konfiguracji, gdy nie podano w żądaniu.
        username = string.IsNullOrWhiteSpace(username) ? _options.Username : username;
        password = string.IsNullOrWhiteSpace(password) ? _options.Password : password;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var (userId, refreshToken, accessToken) = await RunAsync(username, password, cancellationToken);

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                _store.SetRefreshToken(userId, refreshToken, clientId: null);
                _logger.LogInformation("Auto-login Frisco: zapisano refresh_token (userId {UserId}).", userId);
                return new FriscoLoginResult(userId, HasRefreshToken: true);
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _store.SetAccessToken(userId, accessToken);
                _logger.LogInformation("Auto-login Frisco: zapisano access_token (userId {UserId}).", userId);
                return new FriscoLoginResult(userId, HasRefreshToken: false);
            }

            throw new FriscoLoginException("Zalogowano, ale nie przechwycono tokenu (ani refresh, ani access).");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(string UserId, string? RefreshToken, string? AccessToken)> RunAsync(
        string? username,
        string? password,
        CancellationToken cancellationToken)
    {
        var credentialMode = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
        var headless = credentialMode && _options.Headless;

        var userDataDir = string.IsNullOrWhiteSpace(_options.UserDataDir)
            ? Path.Combine(Path.GetTempPath(), "zakupogarniacz-frisco-profile")
            : _options.UserDataDir;

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var context = await playwright.Chromium.LaunchPersistentContextAsync(
            userDataDir,
            new BrowserTypeLaunchPersistentContextOptions { Headless = headless });

        var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();

        // Przechwytywanie tokenu: odpowiedź /connect/token zawsze zawiera refresh/access.
        var tokenTcs = new TaskCompletionSource<(string? Refresh, string? Access)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        string? userId = null;
        string? headerAccessToken = null;

        // Nasłuch tokenu/usera podpinamy do strony i do każdej NOWEJ karty/popupu (logowanie OAuth
        // bywa w osobnym oknie) — inaczej pojedynczy nasłuch przegapia token.
        void Attach(IPage p)
        {
            p.Request += (_, request) =>
            {
                var match = UserIdRegex().Match(request.Url);
                if (match.Success)
                {
                    userId ??= match.Groups[1].Value;
                }

                // Fallback: gdy jesteś zalogowany, SPA i tak woła API z Authorization: Bearer.
                if (headerAccessToken is null
                    && request.Url.Contains("/commerce/api/", StringComparison.OrdinalIgnoreCase))
                {
                    _ = CaptureAuthHeaderAsync(request, token => headerAccessToken ??= token);
                }
            };

            p.Response += (_, response) =>
            {
                if (response.Url.Contains("/connect/token", StringComparison.OrdinalIgnoreCase))
                {
                    _ = CaptureTokenAsync(response, tokenTcs);
                }
            };
        }

        Attach(page);
        context.Page += (_, p) => Attach(p);

        await page.GotoAsync(
            _options.LoginUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        await TryDismissCookieBannerAsync(page);

        if (credentialMode)
        {
            await FillAndSubmitAsync(page, username!, password!);
            await ThrowIfManualStepAsync(page);
        }
        else
        {
            _logger.LogInformation(
                "Auto-login Frisco (interaktywny): zaloguj się w otwartym oknie — czekam do {Seconds}s.",
                _options.TimeoutMs / 1000);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.TimeoutMs);

        // Pętla przechwytywania (priorytet: refresh_token). Źródła: odpowiedź /connect/token,
        // storage przeglądarki, a gdy okno otwiera się już zalogowane — wymuszamy reload, by
        // sprowokować świeży /connect/token. Access-token z nagłówka to fallback (10 min, bez odnawiania).
        (string? Refresh, string? Access)? capture = null;
        var reloaded = false;
        var elapsedSeconds = 0;
        try
        {
            while (!timeoutCts.IsCancellationRequested)
            {
                if (tokenTcs.Task.IsCompletedSuccessfully)
                {
                    capture = tokenTcs.Task.Result;
                    _logger.LogInformation("Auto-login Frisco: token z odpowiedzi /connect/token.");
                    break;
                }

                foreach (var p in context.Pages)
                {
                    var fromStorage = await ScanStorageAsync(p);
                    if (fromStorage is not null)
                    {
                        capture = fromStorage;
                        _logger.LogInformation("Auto-login Frisco: token ze storage przeglądarki.");
                        break;
                    }
                }

                if (capture is not null)
                {
                    break;
                }

                // Okno otwarte już zalogowane → po ~3s wymuś reload, by sprowokować odnowienie tokenu.
                if (!reloaded && elapsedSeconds >= 3 && !credentialMode)
                {
                    reloaded = true;
                    try
                    {
                        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                    }
                    catch
                    {
                        // Reload może się nie udać (nawigacja) — nieistotne, lecimy dalej.
                    }
                }

                // Mamy już access-token z nagłówka i daliśmy reloadowi szansę na refresh — nie czekamy dłużej.
                if (headerAccessToken is not null && elapsedSeconds >= 8)
                {
                    break;
                }

                await Task.Delay(1000, timeoutCts.Token);
                elapsedSeconds++;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout — capture zostaje null, obsłużone niżej.
        }

        // Fallback: access-token wyłuskany z nagłówka Authorization (gdy nie złapaliśmy refresh).
        if (capture is null && !string.IsNullOrWhiteSpace(headerAccessToken))
        {
            capture = (null, headerAccessToken);
            _logger.LogInformation("Auto-login Frisco: access-token z nagłówka Authorization (tryb access, ~10 min).");
        }

        if (capture is null)
        {
            throw new FriscoLoginException(
                "Nie przechwycono tokenu (minął limit czasu). Jeśli jesteś już zalogowany w oknie, " +
                "kliknij coś w sklepie Frisco (np. koszyk) — to wymusi żądanie z tokenem, który złapię.");
        }

        var (refresh, access) = capture.Value;
        userId ??= UserIdFromJwt(access ?? refresh)
            ?? throw new FriscoLoginException("Przechwycono token, ale nie ustaliłem userId (z URL ani z JWT).");

        return (userId, refresh, access);
    }

    private static async Task CaptureAuthHeaderAsync(IRequest request, Action<string> onToken)
    {
        try
        {
            var headers = await request.AllHeadersAsync();
            if (headers.TryGetValue("authorization", out var auth)
                && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth["Bearer ".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    onToken(token);
                }
            }
        }
        catch
        {
            // Best-effort: nagłówków czasem nie da się odczytać (żądanie już poszło).
        }
    }

    private static async Task CaptureTokenAsync(IResponse response, TaskCompletionSource<(string?, string?)> tcs)
    {
        try
        {
            var text = await response.TextAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var refresh = root.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
            var access = root.TryGetProperty("access_token", out var a) ? a.GetString() : null;
            if (!string.IsNullOrWhiteSpace(refresh) || !string.IsNullOrWhiteSpace(access))
            {
                tcs.TrySetResult((refresh, access));
            }
        }
        catch
        {
            // Best-effort: nie każda odpowiedź /connect/token da się odczytać (np. preflight).
        }
    }

    private async Task FillAndSubmitAsync(IPage page, string username, string password)
    {
        var userField = await FirstVisibleAsync(page, _options.UsernameSelectors)
            ?? throw new FriscoLoginException("Nie znalazłem pola loginu (dostosuj FriscoCheckout:AutoLogin:UsernameSelectors).");
        await userField.FillAsync(username);

        var passField = await FirstVisibleAsync(page, _options.PasswordSelectors)
            ?? throw new FriscoLoginException("Nie znalazłem pola hasła (dostosuj FriscoCheckout:AutoLogin:PasswordSelectors).");
        await passField.FillAsync(password);

        var submit = await FirstVisibleAsync(page, _options.SubmitSelectors);
        if (submit is not null)
        {
            await submit.ClickAsync();
        }
        else
        {
            await passField.PressAsync("Enter");
        }
    }

    private static async Task<IElementHandle?> FirstVisibleAsync(IPage page, string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element is not null && await element.IsVisibleAsync())
            {
                return element;
            }
        }

        return null;
    }

    private static async Task TryDismissCookieBannerAsync(IPage page)
    {
        // Prywatność: odrzucamy zgody nie-niezbędne. Best-effort — brak bannera nie jest błędem.
        string[] labels = ["Tylko niezbędne", "Odrzuć wszystko", "Odrzuć", "Nie zgadzam się"];
        foreach (var label in labels)
        {
            try
            {
                var locator = page.Locator($"button:has-text(\"{label}\")");
                if (await locator.CountAsync() > 0)
                {
                    await locator.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    return;
                }
            }
            catch
            {
                // Ignorujemy — banner może nie istnieć albo mieć inny układ.
            }
        }
    }

    private static async Task ThrowIfManualStepAsync(IPage page)
    {
        var captcha = await page.QuerySelectorAsync(
            "iframe[src*='recaptcha'], iframe[src*='hcaptcha'], iframe[title*='captcha'], [data-testid*='captcha']");
        if (captcha is not null)
        {
            throw new FriscoManualStepRequiredException(
                "Frisco pokazał captchę/weryfikację — automat tego nie obchodzi. Użyj trybu interaktywnego " +
                "i dokończ logowanie ręcznie w oknie.");
        }
    }

    private static async Task<(string?, string?)?> ScanStorageAsync(IPage page)
    {
        try
        {
            var json = await page.EvaluateAsync<string>(
                """
                () => {
                  const found = [];
                  const consider = (j) => { if (j && typeof j === 'object') { if (j.refresh_token || j.access_token) found.push(j); for (const k in j) { try { if (j[k] && typeof j[k] === 'object') consider(j[k]); } catch (e) {} } } };
                  const grab = (s) => { for (let i = 0; i < s.length; i++) { try { consider(JSON.parse(s.getItem(s.key(i)))); } catch (e) {} } };
                  grab(localStorage); grab(sessionStorage);
                  return JSON.stringify(found);
                }
                """);

            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var refresh = element.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
                var access = element.TryGetProperty("access_token", out var a) ? a.GetString() : null;
                if (!string.IsNullOrWhiteSpace(refresh) || !string.IsNullOrWhiteSpace(access))
                {
                    return (refresh, access);
                }
            }
        }
        catch
        {
            // Storage niedostępny / inny format — fallback po prostu nie zadziała.
        }

        return null;
    }

    private static string? UserIdFromJwt(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(DecodeJwtSegment(parts[1]));
            var root = doc.RootElement;
            foreach (var claim in (ReadOnlySpan<string>)["sub", "userId", "user_id", "nameid"])
            {
                if (root.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text) && text.All(char.IsAsciiDigit))
                    {
                        return text;
                    }
                }
            }
        }
        catch
        {
            // Token nie jest dekodowalnym JWT — trudno, userId weźmiemy skądinąd albo zgłosimy błąd.
        }

        return null;
    }

    private static string DecodeJwtSegment(string segment)
    {
        var normalized = segment.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    [GeneratedRegex(@"/users/(\d+)(?:/|\b)", RegexOptions.IgnoreCase)]
    private static partial Regex UserIdRegex();
}
