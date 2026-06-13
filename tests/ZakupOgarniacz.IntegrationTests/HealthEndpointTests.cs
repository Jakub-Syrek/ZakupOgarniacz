using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ZakupOgarniacz.IntegrationTests;

/// <summary>
/// Smoke-test integracyjny: podnosi cały host API w pamięci
/// (<see cref="WebApplicationFactory{TEntryPoint}"/>) i sprawdza endpoint
/// <c>/health</c>. Potwierdza, że DI i pipeline HTTP spinają się poprawnie.
/// </summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_zwraca_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
