using System.Net;
using System.Net.Http.Headers;
using Banking.NET.Commerzbank.Internal;

namespace Banking.NET.Commerzbank.Auth;

/// <summary>A <see cref="DelegatingHandler"/> that attaches a bearer access token to every request and retries once with a fresh token when the gateway rejects it.</summary>
public sealed class CommerzbankAuthHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;

    /// <summary>Initializes a new instance using the given token provider.</summary>
    /// <param name="tokenProvider">The provider used to obtain and invalidate access tokens.</param>
    public CommerzbankAuthHandler(IAccessTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Content is not null)
            await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!IsTokenRejected(response))
            return response;

        response.Dispose();
        _tokenProvider.Invalidate();

        using var retryRequest = await CloneAsync(request, cancellationToken).ConfigureAwait(false);
        var freshToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);

        return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }

        return clone;
    }

    internal static bool IsTokenRejected(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return true;

        if (response.StatusCode != HttpStatusCode.BadRequest)
            return false;

        var parameter = CommerzbankErrorHandler.TryGetBearerChallengeParameter(response, "error");
        return string.Equals(parameter, "invalid_token", StringComparison.Ordinal);
    }
}
