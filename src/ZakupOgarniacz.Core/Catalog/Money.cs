namespace ZakupOgarniacz.Core.Catalog;

/// <summary>Kwota pieniężna w danej walucie (domyślnie PLN).</summary>
public sealed record Money(decimal Amount, string Currency = "PLN")
{
    public Money Multiply(int quantity) => this with { Amount = Amount * quantity };

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
