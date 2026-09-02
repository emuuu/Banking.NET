using System.Net;

namespace Commerzbank.NET.Exceptions;

/// <summary>Thrown when the OAuth token endpoint rejects credentials, or when the API gateway rejects a request due to a missing or invalid bearer token (401, or 400 with a `WWW-Authenticate` challenge).</summary>
public class CommerzbankAuthenticationException : CommerzbankException
{
    /// <summary>Initializes a new instance with a message only.</summary>
    /// <param name="message">The exception message.</param>
    public CommerzbankAuthenticationException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with full HTTP response context.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code returned by the gateway.</param>
    /// <param name="correlationId">The value of the `X-CorrelationID` response header.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public CommerzbankAuthenticationException(string message, HttpStatusCode? statusCode, string? correlationId, string? rawResponse, Exception? innerException = null)
        : base(message, statusCode, correlationId, rawResponse, innerException)
    {
    }
}
