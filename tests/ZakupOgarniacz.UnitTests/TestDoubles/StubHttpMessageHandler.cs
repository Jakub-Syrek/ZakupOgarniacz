using System.Net;
using System.Text;

namespace ZakupOgarniacz.UnitTests.TestDoubles;

/// <summary>Handler HTTP zwracający ustaloną odpowiedź; zapamiętuje URL ostatniego żądania.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json;
    private readonly HttpStatusCode _statusCode;

    public StubHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _json = json;
        _statusCode = statusCode;
    }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json"),
        });
    }
}
