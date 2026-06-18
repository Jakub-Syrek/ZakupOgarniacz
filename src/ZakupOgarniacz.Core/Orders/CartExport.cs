namespace ZakupOgarniacz.Core.Orders;

/// <summary>Pojedyncza pozycja wyeksportowanego koszyka (lista zakupów do sklepu).</summary>
public sealed record CartExportLine(string ProductName, int Quantity, string? ProductUrl);

/// <summary>
/// Wynik eksportu koszyka — etap 1 „zamawiania": lista produktów (z linkami do sklepu),
/// którą użytkownik finalizuje w sklepie. Docelowo zastąpi/uzupełni to auto-checkout.
/// </summary>
public sealed record CartExport(IReadOnlyList<CartExportLine> Lines);
