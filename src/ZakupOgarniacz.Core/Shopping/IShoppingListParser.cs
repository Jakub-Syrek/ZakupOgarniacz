namespace ZakupOgarniacz.Core.Shopping;

/// <summary>Pojedyncza pozycja wyodrębniona z polecenia: fraza do wyszukania + ilość.</summary>
public sealed record ShoppingItem(string Query, int Quantity);

/// <summary>
/// Zamienia polecenie w naturalnym języku (np. „kup mi 3 bochenki ciemnego pieczywa,
/// czekoladę i pomarańcze") na listę pozycji do wyszukania w katalogu. Wymienialny —
/// implementacja oparta o LLM jest za tym interfejsem.
/// </summary>
public interface IShoppingListParser
{
    /// <summary>Czy parser jest skonfigurowany (np. czy ma klucz API).</summary>
    bool IsConfigured { get; }

    Task<IReadOnlyList<ShoppingItem>> ParseAsync(string command, CancellationToken cancellationToken = default);
}
