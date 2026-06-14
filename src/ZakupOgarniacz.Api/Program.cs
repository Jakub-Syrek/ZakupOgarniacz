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

app.Run();

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
