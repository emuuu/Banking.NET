using System.Net;
using System.Text.RegularExpressions;
using Banking.NET.Commerzbank.Exceptions;

namespace Banking.NET.Commerzbank.Internal;

internal static class CommerzbankErrorHandler
{
    private const int MaxBodyLength = 500;

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
            return;

        throw await CreateExceptionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<CommerzbankException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var correlationId = GetCorrelationId(response);
        var errorDescription = TryGetBearerChallengeParameter(response, "error_description");
        var trimmedBody = body.Trim();
        var detail = errorDescription;
        if (string.IsNullOrEmpty(detail))
            detail = trimmedBody.Length > MaxBodyLength ? trimmedBody[..MaxBodyLength] : trimmedBody;
        if (string.IsNullOrEmpty(detail))
            detail = "no details";

        var message = $"Commerzbank API returned {(int)response.StatusCode} {response.ReasonPhrase}: {detail}";
        var hasBearerChallenge = HasBearerChallenge(response);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new CommerzbankAuthenticationException(message, response.StatusCode, correlationId, body),
            HttpStatusCode.BadRequest when hasBearerChallenge => new CommerzbankAuthenticationException(message, response.StatusCode, correlationId, body),
            HttpStatusCode.BadRequest => new CommerzbankBadRequestException(message, response.StatusCode, correlationId, body),
            HttpStatusCode.NotFound => new CommerzbankNotFoundException(message, response.StatusCode, correlationId, body),
            HttpStatusCode.Gone => new CommerzbankGoneException(message, response.StatusCode, correlationId, body),
            _ => new CommerzbankApiException(message, response.StatusCode, correlationId, body),
        };
    }

    public static string? GetCorrelationId(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Headers.TryGetValues("X-CorrelationID", out var values) ? values.FirstOrDefault() : null;
    }

    public static string? TryGetBearerChallengeParameter(HttpResponseMessage response, string name)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        foreach (var challenge in response.Headers.WwwAuthenticate)
        {
            if (!string.Equals(challenge.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || challenge.Parameter is null)
                continue;

            var match = Regex.Match(challenge.Parameter, $"{Regex.Escape(name)}=\"([^\"]*)\"");
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    private static bool HasBearerChallenge(HttpResponseMessage response) =>
        response.Headers.WwwAuthenticate.Any(challenge => string.Equals(challenge.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase));
}
