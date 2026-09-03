namespace Banking.NET.Commerzbank.Auth;

/// <summary>Provides OAuth 2.0 access tokens used to authenticate requests against the Commerzbank Corporate Payments API.</summary>
public interface IAccessTokenProvider
{
    /// <summary>Gets a valid access token, obtaining or refreshing one when necessary.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A valid bearer access token.</returns>
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops cached tokens so that the next call obtains a fresh one.</summary>
    void Invalidate();
}
