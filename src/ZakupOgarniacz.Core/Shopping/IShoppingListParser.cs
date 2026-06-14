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

    /// <summary>
    /// Zamienia polecenie na listę pozycji. <paramref name="preferences"/> to ulubione
    /// (marki/produkty), które parser ma traktować priorytetowo przy wyborze.
    /// </summary>
    Task<IReadOnlyList<ShoppingItem>> ParseAsync(
        string command,
        IReadOnlyList<string> preferences,
        CancellationToken cancellationToken = default);
}
