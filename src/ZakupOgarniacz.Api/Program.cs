var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — konfiguracja: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Prosty health-check. Właściwe endpointy katalogu (search/product)
// dochodzą w kroku 1 roadmapy — za interfejsem ICatalogProvider.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("HealthCheck");

app.Run();

// Udostępnione dla testów integracyjnych (WebApplicationFactory<Program>).
public partial class Program { }
