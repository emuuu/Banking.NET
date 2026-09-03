using System.Net;
using System.Net.Http.Headers;
using Banking.NET.Commerzbank.Exceptions;
using Banking.NET.Commerzbank.Internal;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Exceptions;

public class CommerzbankErrorHandlerTests
{
    private static HttpResponseMessage Response(HttpStatusCode statusCode, string? body = null, string? correlationId = null, string? bearerChallenge = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body ?? string.Empty),
        };

        if (correlationId is not null)
            response.Headers.Add("X-CorrelationID", correlationId);

        if (bearerChallenge is not null)
            response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", bearerChallenge));

        return response;
    }

    [Fact]
    public async Task EnsureSuccessAsync_200_DoesNotThrow()
    {
        using var response = Response(HttpStatusCode.OK);
        await Should.NotThrowAsync(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_204_DoesNotThrow()
    {
        using var response = Response(HttpStatusCode.NoContent);
        await Should.NotThrowAsync(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_400WithBearerChallenge_ThrowsAuthenticationException()
    {
        using var response = Response(HttpStatusCode.BadRequest, bearerChallenge: "realm=\"DefaultRealm\", error=\"invalid_request\", error_description=\"Unable to find token in the message\"");
        await Should.ThrowAsync<CommerzbankAuthenticationException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_400WithoutChallenge_ThrowsBadRequestException()
    {
        using var response = Response(HttpStatusCode.BadRequest, body: "Invalid OrderType header");
        await Should.ThrowAsync<CommerzbankBadRequestException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_401_ThrowsAuthenticationException()
    {
        using var response = Response(HttpStatusCode.Unauthorized);
        await Should.ThrowAsync<CommerzbankAuthenticationException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_404_ThrowsNotFoundException()
    {
        using var response = Response(HttpStatusCode.NotFound);
        await Should.ThrowAsync<CommerzbankNotFoundException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_410_ThrowsGoneException()
    {
        using var response = Response(HttpStatusCode.Gone);
        await Should.ThrowAsync<CommerzbankGoneException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureSuccessAsync_500_ThrowsApiException()
    {
        using var response = Response(HttpStatusCode.InternalServerError);
        await Should.ThrowAsync<CommerzbankApiException>(() => CommerzbankErrorHandler.EnsureSuccessAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task CreateExceptionAsync_NotFoundWithCorrelationId_SetsStatusCodeAndCorrelationId()
    {
        using var response = Response(HttpStatusCode.NotFound, correlationId: "Id-abc123");
        var exception = await CommerzbankErrorHandler.CreateExceptionAsync(response, CancellationToken.None);

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.CorrelationId.ShouldBe("Id-abc123");
    }

    [Fact]
    public async Task CreateExceptionAsync_BearerChallengeWithErrorDescription_UsesErrorDescriptionAsDetail()
    {
        using var response = Response(HttpStatusCode.Unauthorized, body: "ignored body", bearerChallenge: "realm=\"DefaultRealm\", error=\"invalid_token\", error_description=\"The access token expired\"");
        var exception = await CommerzbankErrorHandler.CreateExceptionAsync(response, CancellationToken.None);

        exception.Message.ShouldContain("The access token expired");
    }

    [Fact]
    public async Task CreateExceptionAsync_BodyLongerThan500Characters_TruncatesDetailTo500Characters()
    {
        var longBody = new string('x', 600);
        using var response = Response(HttpStatusCode.InternalServerError, body: longBody);

        var exception = await CommerzbankErrorHandler.CreateExceptionAsync(response, CancellationToken.None);

        exception.RawResponse.ShouldBe(longBody);
        exception.Message.ShouldContain(new string('x', 500));
        exception.Message.ShouldNotContain(new string('x', 501));
    }

    [Fact]
    public async Task CreateExceptionAsync_EmptyBodyAndNoChallenge_UsesNoDetailsFallback()
    {
        using var response = Response(HttpStatusCode.InternalServerError);
        var exception = await CommerzbankErrorHandler.CreateExceptionAsync(response, CancellationToken.None);

        exception.Message.ShouldContain("no details");
    }

    [Fact]
    public void GetCorrelationId_MissingHeader_ReturnsNull()
    {
        using var response = Response(HttpStatusCode.OK);
        CommerzbankErrorHandler.GetCorrelationId(response).ShouldBeNull();
    }

    [Fact]
    public void TryGetBearerChallengeParameter_MissingChallenge_ReturnsNull()
    {
        using var response = Response(HttpStatusCode.BadRequest);
        CommerzbankErrorHandler.TryGetBearerChallengeParameter(response, "error").ShouldBeNull();
    }

    [Fact]
    public void TryGetBearerChallengeParameter_PresentChallenge_ReturnsValue()
    {
        using var response = Response(HttpStatusCode.BadRequest, bearerChallenge: "realm=\"DefaultRealm\", error=\"invalid_token\", error_description=\"expired\"");
        CommerzbankErrorHandler.TryGetBearerChallengeParameter(response, "error").ShouldBe("invalid_token");
    }
}
