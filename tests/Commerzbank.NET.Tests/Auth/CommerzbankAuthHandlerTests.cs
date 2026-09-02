using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Commerzbank.NET.Auth;
using Commerzbank.NET.Tests.Helpers;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Auth;

public class CommerzbankAuthHandlerTests
{
    private static IAccessTokenProvider TokenProviderReturning(params string[] tokens)
    {
        var provider = Substitute.For<IAccessTokenProvider>();
        provider.GetAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(tokens[0]), tokens.Skip(1).Select(ValueTask.FromResult).ToArray());
        return provider;
    }

    private static HttpResponseMessage InvalidTokenChallenge() =>
        new(HttpStatusCode.BadRequest)
        {
            Headers = { WwwAuthenticate = { new AuthenticationHeaderValue("Bearer", "realm=\"DefaultRealm\", error=\"invalid_token\", error_description=\"The access token expired\"") } },
        };

    private static HttpResponseMessage InvalidRequestChallenge() =>
        new(HttpStatusCode.BadRequest)
        {
            Headers = { WwwAuthenticate = { new AuthenticationHeaderValue("Bearer", "realm=\"DefaultRealm\", error=\"invalid_request\", error_description=\"Unable to find token in the message\"") } },
        };

    [Fact]
    public async Task SendAsync_AttachesBearerToken()
    {
        var innerHandler = new ScriptedHttpMessageHandler().Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var tokenProvider = TokenProviderReturning("token1");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        await httpClient.GetAsync("https://api.example.com/messages");

        var request = innerHandler.Requests.Single();
        request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization!.Parameter.ShouldBe("token1");
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_DoesNotRetry()
    {
        var innerHandler = new ScriptedHttpMessageHandler().Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var tokenProvider = TokenProviderReturning("token1");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        var response = await httpClient.GetAsync("https://api.example.com/messages");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(1);
        tokenProvider.DidNotReceive().Invalidate();
    }

    [Fact]
    public async Task SendAsync_401Response_InvalidatesAndRetriesOnceWithFreshTokenResendingContent()
    {
        var innerHandler = new ScriptedHttpMessageHandler()
            .Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var tokenProvider = TokenProviderReturning("token1", "token2");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        var response = await httpClient.PostAsync("https://api.example.com/messages", new StringContent("<xml/>", Encoding.UTF8, "application/xml"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests[0].Headers.Authorization!.Parameter.ShouldBe("token1");
        innerHandler.Requests[1].Headers.Authorization!.Parameter.ShouldBe("token2");
        innerHandler.RequestContents[0].ShouldBe("<xml/>");
        innerHandler.RequestContents[1].ShouldBe("<xml/>");
        innerHandler.Requests[1].Content!.Headers.ContentType!.MediaType.ShouldBe("application/xml");
        tokenProvider.Received(1).Invalidate();
    }

    [Fact]
    public async Task SendAsync_400WithInvalidTokenChallenge_Retries()
    {
        var innerHandler = new ScriptedHttpMessageHandler()
            .Enqueue(InvalidTokenChallenge())
            .Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var tokenProvider = TokenProviderReturning("token1", "token2");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        var response = await httpClient.GetAsync("https://api.example.com/messages");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SendAsync_400WithInvalidRequestChallenge_DoesNotRetry()
    {
        var innerHandler = new ScriptedHttpMessageHandler().Enqueue(InvalidRequestChallenge());
        var tokenProvider = TokenProviderReturning("token1");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        var response = await httpClient.GetAsync("https://api.example.com/messages");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        innerHandler.Requests.Count.ShouldBe(1);
        tokenProvider.DidNotReceive().Invalidate();
    }

    [Fact]
    public async Task SendAsync_SecondResponseAlsoUnauthorized_ReturnsItWithoutThirdAttempt()
    {
        var innerHandler = new ScriptedHttpMessageHandler()
            .Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var tokenProvider = TokenProviderReturning("token1", "token2");
        using var authHandler = new CommerzbankAuthHandler(tokenProvider) { InnerHandler = innerHandler };
        using var httpClient = new HttpClient(authHandler);

        var response = await httpClient.GetAsync("https://api.example.com/messages");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        innerHandler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public void IsTokenRejected_Unauthorized_ReturnsTrue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        CommerzbankAuthHandler.IsTokenRejected(response).ShouldBeTrue();
    }

    [Fact]
    public void IsTokenRejected_BadRequestWithoutChallenge_ReturnsFalse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        CommerzbankAuthHandler.IsTokenRejected(response).ShouldBeFalse();
    }

    [Fact]
    public void IsTokenRejected_OtherStatusCode_ReturnsFalse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        CommerzbankAuthHandler.IsTokenRejected(response).ShouldBeFalse();
    }

    [Fact]
    public async Task CloneAsync_CopiesMethodUriHeadersAndContent()
    {
        using var original = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/messages")
        {
            Content = new StringContent("payload", Encoding.UTF8, "application/xml"),
        };
        original.Headers.Add("ClientProduct", "MyErp/2.4");
        await original.Content.LoadIntoBufferAsync(TestContext.Current.CancellationToken);

        using var clone = await CommerzbankAuthHandler.CloneAsync(original, TestContext.Current.CancellationToken);

        clone.Method.ShouldBe(original.Method);
        clone.RequestUri.ShouldBe(original.RequestUri);
        clone.Headers.GetValues("ClientProduct").ShouldContain("MyErp/2.4");
        (await clone.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("payload");
        clone.Content.Headers.ContentType!.MediaType.ShouldBe("application/xml");
    }
}
