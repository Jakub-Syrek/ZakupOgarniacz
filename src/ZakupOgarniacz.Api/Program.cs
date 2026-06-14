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

// Token edytowalny w locie: ustaw / sprawdź status (token tylko w pamięci procesu).
app.MapPost("/frisco/token", (SetFriscoTokenRequest request, FriscoCredentialStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.AccessToken))
    {
        return Results.BadRequest(new { error = "userId i accessToken są wymagane." });
    }

    store.Set(request.UserId, request.AccessToken, request.AuthScheme);
    return Results.Ok(new { configured = true });
})
.WithName("SetFriscoToken")
.WithTags("FriscoCheckout");

app.MapGet("/frisco/token/status", (FriscoCredentialStore store) =>
    Results.Ok(new { configured = store.IsConfigured }))
   .WithName("FriscoTokenStatus")
   .WithTags("FriscoCheckout");

app.Run();

// Żądanie ustawienia tokena Frisco (auto-checkout).
internal sealed record SetFriscoTokenRequest(string UserId, string AccessToken, string? AuthScheme);

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
