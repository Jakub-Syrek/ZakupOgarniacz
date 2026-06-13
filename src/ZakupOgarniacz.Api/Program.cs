using System.Text.Json.Serialization;
using ZakupOgarniacz.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — konfiguracja: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Katalog produktów (adapter Open Food Facts za ICatalogProvider).
builder.Services.AddOpenFoodFactsCatalog(builder.Configuration);

// NutriScore i inne enumy serializujemy jako string ("A".."E", "Unknown").
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Prosty health-check.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("HealthCheck");

// Endpointy katalogu: /products/search, /products/{code}, /products/{code}/nutrition.
app.MapCatalogEndpoints();

app.Run();

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
