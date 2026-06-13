using System.Net;
using System.Text;

namespace ZakupOgarniacz.UnitTests.TestDoubles;

/// <summary>
/// Prosty <see cref="HttpMessageHandler"/> zwracający z góry ustaloną odpowiedź.
/// Zapamiętuje ostatnie żądanie, żeby testy mogły sprawdzić budowany URL.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _json;

    public StubHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _json = json;
        _statusCode = statusCode;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
