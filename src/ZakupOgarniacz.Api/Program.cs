using ZakupOgarniacz.Api.Endpoints;
using ZakupOgarniacz.Providers.Frisco;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — konfiguracja: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Sklep Frisco.pl (katalog + zamawianie) za ICatalogProvider / IOrderProvider.
builder.Services.AddFriscoStore(builder.Configuration);

// Magazyn koszyków (EF Core + SQLite).
builder.Services.AddSqliteCartStore(
    builder.Configuration.GetConnectionString("Carts") ?? "Data Source=carts.db");

// Zalogowany klient Frisco (auto-checkout do ekranu płatności) — token z konfiguracji.
builder.Services.AddFriscoCheckout(builder.Configuration);

var app = builder.Build();

// Pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// UI (statyczny SPA z wwwroot): "/" -> index.html.
app.UseDefaultFiles();
app.UseStaticFiles();

// Health-check.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("HealthCheck");

// Endpointy katalogu: /products/search, /products/{code}.
app.MapCatalogEndpoints();

// Endpointy koszyka: /carts (create/get/items/export).
app.MapCartEndpoints();

// Dev/weryfikacja: podgląd koszyka Frisco (wymaga skonfigurowanego FriscoCheckout: UserId + AccessToken).
app.MapGet("/frisco/cart", async (FriscoCheckoutClient client, CancellationToken cancellationToken) =>
{
    if (!client.IsConfigured)
    {
        return Results.Problem("Brak konfiguracji FriscoCheckout (UserId/AccessToken).", statusCode: 503);
    }

    try
    {
        var json = await client.GetCartRawAsync(cancellationToken);
        return Results.Content(json, "application/json");
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem("Frisco: " + ex.Message, statusCode: 502);
    }
})
.WithName("FriscoCartRaw")
.WithTags("FriscoCheckout");

// Terminy dostawy + opcje płatności dla kodu pocztowego (odczyt; pod wybór slotu).
app.MapGet("/frisco/delivery", async (string postcode, FriscoCheckoutClient client, CancellationToken cancellationToken) =>
{
    if (!client.IsConfigured)
    {
        return Results.Problem("Brak konfiguracji FriscoCheckout (token).", statusCode: 503);
    }

    if (string.IsNullOrWhiteSpace(postcode))
    {
        return Results.BadRequest(new { error = "Parametr 'postcode' jest wymagany." });
    }

    try
    {
        var json = await client.GetDeliveryPaymentRawAsync(postcode, cancellationToken);
        return Results.Content(json, "application/json");
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem("Frisco: " + ex.Message, statusCode: 502);
    }
})
.WithName("FriscoDelivery")
.WithTags("FriscoCheckout");

// Read-proxy (dev/recon): autoryzowany GET na users/{id}/{path}. Tylko odczyt.
app.MapGet("/frisco/raw", async (string path, FriscoCheckoutClient client, CancellationToken cancellationToken) =>
{
    if (!client.IsConfigured)
    {
        return Results.Problem("Brak konfiguracji FriscoCheckout (token).", statusCode: 503);
    }

    if (string.IsNullOrWhiteSpace(path))
    {
        return Results.BadRequest(new { error = "Parametr 'path' jest wymagany." });
    }

    try
    {
        var result = await client.GetUserScopedRawAsync(path, cancellationToken);
        return Results.Content(result.Body, "application/json", null, result.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem("Frisco: " + ex.Message, statusCode: 502);
    }
})
.WithName("FriscoRaw")
.WithTags("FriscoCheckout");

// Poświadczenia edytowalne w locie (tylko w pamięci): refresh_token (zalecane) lub access-token.
app.MapPost("/frisco/token", (SetFriscoTokenRequest request, FriscoCredentialStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId))
    {
        return Results.BadRequest(new { error = "userId jest wymagany." });
    }

    if (!string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        store.SetRefreshToken(request.UserId, request.RefreshToken, request.ClientId);
        return Results.Ok(new { configured = true, mode = "refresh" });
    }

    if (!string.IsNullOrWhiteSpace(request.AccessToken))
    {
        store.SetAccessToken(request.UserId, request.AccessToken);
        return Results.Ok(new { configured = true, mode = "access" });
    }

    return Results.BadRequest(new { error = "Podaj refreshToken (zalecane) albo accessToken." });
})
.WithName("SetFriscoToken")
.WithTags("FriscoCheckout");

app.MapGet("/frisco/token/status", (FriscoCredentialStore store) =>
    Results.Ok(new { configured = store.IsConfigured }))
   .WithName("FriscoTokenStatus")
   .WithTags("FriscoCheckout");

app.Run();

// Żądanie ustawienia poświadczeń Frisco (auto-checkout). Podaj refreshToken lub accessToken.
internal sealed record SetFriscoTokenRequest(
    string UserId,
    string? AccessToken,
    string? RefreshToken,
    string? ClientId);

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
