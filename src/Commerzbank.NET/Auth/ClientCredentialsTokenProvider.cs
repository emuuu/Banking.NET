using System.Text.Json;
using Commerzbank.NET.Exceptions;
using Commerzbank.NET.Internal;
using Microsoft.Extensions.Options;

namespace Commerzbank.NET.Auth;

/// <summary>Obtains and caches OAuth 2.0 access tokens via the client credentials grant, transparently using a refresh token when available.</summary>
public sealed class ClientCredentialsTokenProvider : IAccessTokenProvider, IDisposable
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly CommerzbankOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private volatile TokenState? _state;

    private sealed record TokenState(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string? RefreshToken, DateTimeOffset? RefreshTokenExpiresAt);

    /// <summary>Initializes a new instance backed by a fixed <see cref="HttpClient"/>. The caller retains ownership of the client.</summary>
    /// <param name="httpClient">The HTTP client used for token requests.</param>
    /// <param name="options">The Commerzbank options providing credentials and the token endpoint.</param>
    /// <param name="timeProvider">The time provider used to evaluate token expiry. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ClientCredentialsTokenProvider(HttpClient httpClient, CommerzbankOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClientFactory = () => httpClient;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Initializes a new instance that creates a new <see cref="HttpClient"/> for each token request via <see cref="CommerzbankHttpClientNames.Token"/>.</summary>
    /// <param name="httpClientFactory">The factory used to create named HTTP clients.</param>
    /// <param name="options">The Commerzbank options providing credentials and the token endpoint.</param>
    /// <param name="timeProvider">The time provider used to evaluate token expiry. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ClientCredentialsTokenProvider(IHttpClientFactory httpClientFactory, IOptions<CommerzbankOptions> options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        _httpClientFactory = () => httpClientFactory.CreateClient(CommerzbankHttpClientNames.Token);
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets a snapshot of the currently cached token, or null if none has been obtained yet.</summary>
    public AccessTokenInfo? CurrentToken
    {
        get
        {
            var state = _state;
            return state is null
                ? null
                : new AccessTokenInfo(state.AccessToken, state.AccessTokenExpiresAt, state.RefreshToken is not null, state.RefreshTokenExpiresAt);
        }
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var state = _state;
        if (state is not null && IsValid(state))
            return state.AccessToken;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            state = _state;
            if (state is not null && IsValid(state))
                return state.AccessToken;

            return await RefreshAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Invalidate() => _state = null;

    /// <inheritdoc />
    public void Dispose() => _semaphore.Dispose();

    private bool IsValid(TokenState state) => state.AccessTokenExpiresAt - _timeProvider.GetUtcNow() > _options.TokenExpiryMargin;

    private async Task<string> RefreshAsync(TokenState? current, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        if (_options.UseRefreshToken && current?.RefreshToken is { } refreshToken && current.RefreshTokenExpiresAt is { } refreshExpiresAt && now < refreshExpiresAt)
        {
            try
            {
                return await RequestTokenAsync(BuildRefreshTokenForm(refreshToken), cancellationToken).ConfigureAwait(false);
            }
            catch (CommerzbankAuthenticationException)
            {
                // The refresh token was rejected; fall back to a new client credentials request.
            }
        }

        return await RequestTokenAsync(BuildClientCredentialsForm(), cancellationToken).ConfigureAwait(false);
    }

    private Dictionary<string, string> BuildClientCredentialsForm() => new()
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = _options.ClientId ?? string.Empty,
        ["client_secret"] = _options.ClientSecret ?? string.Empty,
    };

    private Dictionary<string, string> BuildRefreshTokenForm(string refreshToken) => new()
    {
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken,
        ["client_id"] = _options.ClientId ?? string.Empty,
        ["client_secret"] = _options.ClientSecret ?? string.Empty,
    };

    private async Task<string> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var client = _httpClientFactory();

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.GetTokenEndpointUri())
        {
            Content = new FormUrlEncodedContent(form),
        };

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var correlationId = CommerzbankErrorHandler.GetCorrelationId(response);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryParseError(body);
            var detail = error is null ? body : $"{error.Error} - {error.ErrorDescription}";
            throw new CommerzbankAuthenticationException(
                $"Commerzbank token endpoint returned {(int)response.StatusCode}: {detail}",
                response.StatusCode,
                correlationId,
                body);
        }

        var tokenResponse = JsonSerializer.Deserialize(body, CommerzbankJsonContext.Default.OAuthTokenResponse);
        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            throw new CommerzbankAuthenticationException(
                "Commerzbank token endpoint response did not contain an access token.",
                response.StatusCode,
                correlationId,
                body);
        }

        var accessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn);
        var refreshTokenExpiresAt = tokenResponse.RefreshExpiresIn is { } refreshExpiresIn ? now.AddSeconds(refreshExpiresIn) : (DateTimeOffset?)null;

        _state = new TokenState(tokenResponse.AccessToken, accessTokenExpiresAt, tokenResponse.RefreshToken, refreshTokenExpiresAt);
        return tokenResponse.AccessToken;
    }

    private static OAuthErrorResponse? TryParseError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return JsonSerializer.Deserialize(body, CommerzbankJsonContext.Default.OAuthErrorResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
