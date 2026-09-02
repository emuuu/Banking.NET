using System.Net;
using System.Text;
using Commerzbank.NET.Auth;
using Commerzbank.NET.Exceptions;
using Commerzbank.NET.Tests.Helpers;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Auth;

public class ClientCredentialsTokenProviderTests
{
    private static readonly Uri TokenEndpoint = new("https://api-sandbox.commerzbank.com/auth/realms/sandbox/protocol/openid-connect/token");

    private static HttpResponseMessage TokenResponse(string accessToken, int expiresIn = 900, string? refreshToken = null, int? refreshExpiresIn = null)
    {
        var refreshTokenJson = refreshToken is null ? "" : $"\"refresh_token\":\"{refreshToken}\",";
        var refreshExpiresJson = refreshExpiresIn is null ? "" : $"\"refresh_expires_in\":{refreshExpiresIn},";
        var json = $$"""
            {"access_token":"{{accessToken}}","expires_in":{{expiresIn}},{{refreshExpiresJson}}{{refreshTokenJson}}"token_type":"bearer"}
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage ErrorResponse(HttpStatusCode statusCode, string error, string errorDescription, string? correlationId = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent($$"""{"error":"{{error}}","error_description":"{{errorDescription}}"}""", Encoding.UTF8, "application/json"),
        };

        if (correlationId is not null)
            response.Headers.Add("X-CorrelationID", correlationId);

        return response;
    }

    private static CommerzbankOptions Options(bool useRefreshToken = true, TimeSpan? margin = null) => new()
    {
        ClientId = "test-client",
        ClientSecret = "test-secret",
        UseRefreshToken = useRefreshToken,
        TokenExpiryMargin = margin ?? TimeSpan.FromSeconds(30),
    };

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    [Fact]
    public async Task GetAccessTokenAsync_FirstCall_SendsFormEncodedRequestWithGrantTypeClientIdAndClientSecret()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(TokenResponse("token1"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        var token = await provider.GetAccessTokenAsync();

        token.ShouldBe("token1");
        handler.Requests.Single().RequestUri.ShouldBe(TokenEndpoint);
        handler.Requests.Single().Content!.Headers.ContentType!.MediaType.ShouldBe("application/x-www-form-urlencoded");

        var body = handler.RequestContents.Single()!;
        body.ShouldContain("grant_type=client_credentials");
        body.ShouldContain("client_id=test-client");
        body.ShouldContain("client_secret=test-secret");
    }

    [Fact]
    public async Task GetAccessTokenAsync_CachedValidToken_DoesNotMakeSecondRequest()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(TokenResponse("token1"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        second.ShouldBe("token1");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CurrentToken_AfterSuccessfulRequest_ReflectsCachedState()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(TokenResponse("token1", expiresIn: 900, refreshToken: "refresh1", refreshExpiresIn: 1800));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(), timeProvider);

        await provider.GetAccessTokenAsync();

        var info = provider.CurrentToken;
        info.ShouldNotBeNull();
        info!.AccessToken.ShouldBe("token1");
        info.HasRefreshToken.ShouldBeTrue();
    }

    [Fact]
    public void CurrentToken_BeforeAnyRequest_IsNull()
    {
        using var httpClient = new HttpClient(new ScriptedHttpMessageHandler());
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        provider.CurrentToken.ShouldBeNull();
    }

    [Fact]
    public async Task GetAccessTokenAsync_AfterExpiry_RequestsNewClientCredentialsToken()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1", expiresIn: 60))
            .Enqueue(TokenResponse("token2", expiresIn: 60));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(useRefreshToken: false), timeProvider);

        var first = await provider.GetAccessTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        var second = await provider.GetAccessTokenAsync();

        first.ShouldBe("token1");
        second.ShouldBe("token2");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ExpiredAccessTokenWithValidRefreshToken_UsesRefreshGrant()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1", expiresIn: 60, refreshToken: "refresh1", refreshExpiresIn: 1800))
            .Enqueue(TokenResponse("token2", expiresIn: 60));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(), timeProvider);

        await provider.GetAccessTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        var second = await provider.GetAccessTokenAsync();

        second.ShouldBe("token2");
        handler.RequestContents[1]!.ShouldContain("grant_type=refresh_token");
        handler.RequestContents[1]!.ShouldContain("refresh_token=refresh1");
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshRejected_FallsBackToClientCredentials()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1", expiresIn: 60, refreshToken: "refresh1", refreshExpiresIn: 1800))
            .Enqueue(ErrorResponse(HttpStatusCode.BadRequest, "invalid_grant", "Refresh token expired"))
            .Enqueue(TokenResponse("token3", expiresIn: 900));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(), timeProvider);

        await provider.GetAccessTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        var third = await provider.GetAccessTokenAsync();

        third.ShouldBe("token3");
        handler.Requests.Count.ShouldBe(3);
        handler.RequestContents[1]!.ShouldContain("grant_type=refresh_token");
        handler.RequestContents[2]!.ShouldContain("grant_type=client_credentials");
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshTokenLocallyExpired_SkipsRefreshRequestUsesClientCredentials()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1", expiresIn: 60, refreshToken: "refresh1", refreshExpiresIn: 90))
            .Enqueue(TokenResponse("token2", expiresIn: 60));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(), timeProvider);

        await provider.GetAccessTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(91));
        var second = await provider.GetAccessTokenAsync();

        second.ShouldBe("token2");
        handler.Requests.Count.ShouldBe(2);
        handler.RequestContents[1]!.ShouldContain("grant_type=client_credentials");
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenExpiryMarginIsMaxValue_TreatsTokenAsExpiredWithoutThrowing()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1"))
            .Enqueue(TokenResponse("token2"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(useRefreshToken: false, margin: TimeSpan.MaxValue));

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        first.ShouldBe("token1");
        second.ShouldBe("token2");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_UseRefreshTokenFalse_NeverUsesRefreshGrant()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1", expiresIn: 60, refreshToken: "refresh1", refreshExpiresIn: 1800))
            .Enqueue(TokenResponse("token2", expiresIn: 60));
        using var httpClient = new HttpClient(handler);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options(useRefreshToken: false), timeProvider);

        await provider.GetAccessTokenAsync();
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        await provider.GetAccessTokenAsync();

        handler.RequestContents[1]!.ShouldContain("grant_type=client_credentials");
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenEndpointReturns401_ThrowsAuthenticationExceptionWithMessageAndCorrelationId()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(ErrorResponse(HttpStatusCode.Unauthorized, "invalid_client", "Invalid client or Invalid client credentials", correlationId: "corr-123"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        var exception = await Should.ThrowAsync<CommerzbankAuthenticationException>(() => provider.GetAccessTokenAsync().AsTask());

        exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        exception.CorrelationId.ShouldBe("corr-123");
        exception.Message.ShouldContain("invalid_client");
        exception.Message.ShouldContain("Invalid client or Invalid client credentials");
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenEndpointNetworkFailure_PropagatesExceptionUnwrappedAndLeavesStateNull()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(new HttpRequestException("connection refused"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        await Should.ThrowAsync<HttpRequestException>(() => provider.GetAccessTokenAsync().AsTask());

        provider.CurrentToken.ShouldBeNull();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        using var httpClient = new HttpClient(new ScriptedHttpMessageHandler());
        var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        provider.Dispose();
        Should.NotThrow(provider.Dispose);
    }

    [Fact]
    public async Task Invalidate_DropsCachedToken_ForcesNewRequestOnNextCall()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(TokenResponse("token1"))
            .Enqueue(TokenResponse("token2"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        await provider.GetAccessTokenAsync();
        provider.Invalidate();
        var second = await provider.GetAccessTokenAsync();

        second.ShouldBe("token2");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TenConcurrentCalls_MakeExactlyOneTokenRequest()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(TokenResponse("token1"));
        using var httpClient = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(httpClient, Options());

        var tasks = Enumerable.Range(0, 10).Select(_ => provider.GetAccessTokenAsync().AsTask());
        var tokens = await Task.WhenAll(tasks);

        tokens.ShouldAllBe(token => token == "token1");
        handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAccessTokenAsync_UsesHttpClientFactoryConstructorOverload()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(TokenResponse("token1"));
        using var httpClient = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(CommerzbankHttpClientNames.Token).Returns(httpClient);

        var options = Microsoft.Extensions.Options.Options.Create(Options());
        using var provider = new ClientCredentialsTokenProvider(factory, options);

        var token = await provider.GetAccessTokenAsync();

        token.ShouldBe("token1");
        factory.Received(1).CreateClient(CommerzbankHttpClientNames.Token);
    }
}
