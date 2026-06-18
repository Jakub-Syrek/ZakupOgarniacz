using ZakupOgarniacz.Core.Orders;

namespace ZakupOgarniacz.Providers.Carrefour;

/// <summary>
/// Adapter <see cref="IOrderProvider"/> dla Carrefour. Etap 1: eksport koszyka do listy
/// produktów z linkami do sklepu (finalizację robi człowiek). Auto-checkout dojdzie później.
/// </summary>
public sealed class CarrefourOrderProvider : IOrderProvider
{
    public Task<CartExport> ExportCartAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var lines = cart.Items
            .Select(item => new CartExportLine(item.Product.Name, item.Quantity, item.Product.ProductUrl))
            .ToList();

        return Task.FromResult(new CartExport(lines));
    }
}
