using System.Net;
using System.Net.Http;

namespace StarshipRegistry.Tests;

internal sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
    {
        _responseFactory = responseFactory ?? (_ => new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }
}
