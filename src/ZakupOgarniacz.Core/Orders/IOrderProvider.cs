namespace ZakupOgarniacz.Core.Orders;

/// <summary>
/// Abstrakcja „zamawiania" w sklepie. Etapowo: na start <see cref="ExportCartAsync"/>
/// (deep-link / lista), docelowo dojdzie automatyczne złożenie zamówienia.
/// </summary>
public interface IOrderProvider
{
    /// <summary>Eksportuje koszyk do postaci listy/linków możliwych do otwarcia w sklepie.</summary>
    Task<CartExport> ExportCartAsync(Cart cart, CancellationToken cancellationToken = default);

    // Docelowo (auto-checkout):
    // Task<OrderConfirmation> PlaceOrderAsync(Cart cart, CancellationToken cancellationToken = default);
}
