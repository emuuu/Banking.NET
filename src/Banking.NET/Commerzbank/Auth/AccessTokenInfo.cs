namespace Banking.NET.Commerzbank.Auth;

/// <summary>A snapshot of a cached access token.</summary>
/// <param name="AccessToken">The bearer access token.</param>
/// <param name="ExpiresAt">The point in time the access token expires.</param>
/// <param name="HasRefreshToken">Whether a refresh token is available for this access token.</param>
/// <param name="RefreshTokenExpiresAt">The point in time the refresh token expires, if any.</param>
public sealed record AccessTokenInfo(string AccessToken, DateTimeOffset ExpiresAt, bool HasRefreshToken, DateTimeOffset? RefreshTokenExpiresAt);
