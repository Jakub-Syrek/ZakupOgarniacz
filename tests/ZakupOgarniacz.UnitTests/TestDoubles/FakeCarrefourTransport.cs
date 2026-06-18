using ZakupOgarniacz.Providers.Carrefour;

namespace ZakupOgarniacz.UnitTests.TestDoubles;

/// <summary>Atrapa transportu Carrefour — zwraca z góry ustalony JSON i zapamiętuje ostatnią ścieżkę.</summary>
internal sealed class FakeCarrefourTransport : ICarrefourTransport
{
    private readonly string _json;

    public FakeCarrefourTransport(string json) => _json = json;

    public string? LastPath { get; private set; }

    public Task<string> GetJsonAsync(string relativePathAndQuery, CancellationToken cancellationToken = default)
    {
        LastPath = relativePathAndQuery;
        return Task.FromResult(_json);
    }
}
