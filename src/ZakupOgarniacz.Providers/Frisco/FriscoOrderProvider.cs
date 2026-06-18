using ZakupOgarniacz.Core.Orders;

namespace ZakupOgarniacz.Providers.Frisco;

/// <summary>
/// Adapter <see cref="IOrderProvider"/> dla Frisco. Etap 1: eksport koszyka do listy
/// produktów (finalizację robi człowiek). Auto-checkout dojdzie później.
/// </summary>
public sealed class FriscoOrderProvider : IOrderProvider
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
