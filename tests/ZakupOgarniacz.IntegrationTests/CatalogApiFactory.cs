using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.IntegrationTests.TestDoubles;

namespace ZakupOgarniacz.IntegrationTests;

/// <summary>
/// Fabryka hosta API z podmienionym <see cref="ICatalogProvider"/> na atrapę,
/// dzięki czemu testy endpointów nie zależą od zewnętrznego API.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogProvider>();
            services.AddSingleton<ICatalogProvider, FakeCatalogProvider>();
        });
    }
}
