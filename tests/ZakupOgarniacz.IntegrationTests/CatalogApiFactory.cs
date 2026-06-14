using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZakupOgarniacz.Core.Catalog;
using ZakupOgarniacz.Core.Orders;
using ZakupOgarniacz.Core.Shopping;
using ZakupOgarniacz.Infrastructure.Orders;
using ZakupOgarniacz.IntegrationTests.TestDoubles;

namespace ZakupOgarniacz.IntegrationTests;

/// <summary>
/// Fabryka hosta API z podmienionym <see cref="ICatalogProvider"/> na atrapę oraz
/// koszykiem in-memory — testy endpointów nie zależą od sieci ani od pliku SQLite.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogProvider>();
            services.AddSingleton<ICatalogProvider, FakeCatalogProvider>();

            // Koszyk in-memory zamiast SQLite — izolacja i brak plików DB w testach.
            services.RemoveAll<ICartStore>();
            services.AddSingleton<ICartStore, InMemoryCartStore>();

            // Atrapa parsera zamiast Claude — bez klucza API i bez wołania LLM.
            services.RemoveAll<IShoppingListParser>();
            services.AddSingleton<IShoppingListParser, FakeShoppingListParser>();
        });
    }
}
