namespace Banking.NET.Commerzbank.Auth;

/// <summary>Delegates access token retrieval to a caller-supplied callback (e.g. an external token source shared with other services).</summary>
public sealed class DelegateAccessTokenProvider : IAccessTokenProvider
{
    private readonly Func<CancellationToken, ValueTask<string>> _provider;

    /// <summary>Initializes a new instance wrapping the given callback.</summary>
    /// <param name="provider">The callback invoked to obtain an access token.</param>
    public DelegateAccessTokenProvider(Func<CancellationToken, ValueTask<string>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <inheritdoc />
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => _provider(cancellationToken);

    /// <summary>Does nothing; the caller owns token caching and invalidation.</summary>
    public void Invalidate()
    {
    }
}
