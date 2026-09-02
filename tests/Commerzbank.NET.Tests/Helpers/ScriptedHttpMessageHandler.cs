namespace Commerzbank.NET.Tests.Helpers;

/// <summary>An <see cref="HttpMessageHandler"/> that replays a queue of scripted responses (or exceptions), one per request, and records every request it received.</summary>
internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _script = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string?> RequestContents { get; } = [];

    public ScriptedHttpMessageHandler Enqueue(HttpResponseMessage response)
    {
        _script.Enqueue(() => response);
        return this;
    }

    public ScriptedHttpMessageHandler Enqueue(Exception exception)
    {
        _script.Enqueue(() => throw exception);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestContents.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        if (_script.Count == 0)
            throw new InvalidOperationException("No scripted response left for this request.");

        return _script.Dequeue()();
    }
}
