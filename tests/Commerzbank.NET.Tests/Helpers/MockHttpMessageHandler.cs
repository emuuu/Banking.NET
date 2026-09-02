namespace Commerzbank.NET.Tests.Helpers;

/// <summary>An <see cref="HttpMessageHandler"/> that returns a single canned response and records the last request it received.</summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public MockHttpMessageHandler(HttpResponseMessage response)
        : this(_ => response)
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestContent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestContent = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return _responseFactory(request);
    }
}
