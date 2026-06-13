namespace ZakupOgarniacz.Core.Catalog;

/// <summary>
/// Ocena Nutri-Score produktu (A = najlepsza, E = najgorsza).
/// <see cref="Unknown"/> oznacza brak danych lub kategorię bez przypisanej oceny.
/// </summary>
public enum NutriScore
{
    Unknown = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5,
}
