using ZakupOgarniacz.Core.Shopping;

namespace ZakupOgarniacz.IntegrationTests.TestDoubles;

/// <summary>Atrapa parsera — zwraca stałą listę pozycji, bez wołania LLM.</summary>
internal sealed class FakeShoppingListParser : IShoppingListParser
{
    public bool IsConfigured => true;

    public Task<IReadOnlyList<ShoppingItem>> ParseAsync(
        string command,
        IReadOnlyList<string> preferences,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShoppingItem>>([new ShoppingItem("mleko", 2)]);
}
