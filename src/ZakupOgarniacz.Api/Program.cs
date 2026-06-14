using ZakupOgarniacz.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — konfiguracja: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Sklep Frisco.pl (katalog + zamawianie) za ICatalogProvider / IOrderProvider.
builder.Services.AddFriscoStore(builder.Configuration);

// Magazyn koszyków (EF Core + SQLite).
builder.Services.AddSqliteCartStore(
    builder.Configuration.GetConnectionString("Carts") ?? "Data Source=carts.db");

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

app.Run();

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
