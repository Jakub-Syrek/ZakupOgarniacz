using ZakupOgarniacz.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — konfiguracja: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Sklep Carrefour (katalog + zamawianie) za ICatalogProvider / IOrderProvider.
builder.Services.AddCarrefourStore(builder.Configuration);

var app = builder.Build();

// Pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health-check.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("HealthCheck");

// Endpointy katalogu: /products/search, /products/{code}.
app.MapCatalogEndpoints();

app.Run();

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
