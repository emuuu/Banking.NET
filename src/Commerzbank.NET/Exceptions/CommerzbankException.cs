using System.Net;

namespace Commerzbank.NET.Exceptions;

/// <summary>Base class for all exceptions thrown by the Commerzbank.NET client library.</summary>
public class CommerzbankException : Exception
{
    /// <summary>The HTTP status code returned by the Commerzbank gateway, if the exception originated from an HTTP response.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>The value of the `X-CorrelationID` response header, if present.</summary>
    public string? CorrelationId { get; }

    /// <summary>The raw response body, if available.</summary>
    public string? RawResponse { get; }

    /// <summary>Initializes a new instance with a message only.</summary>
    /// <param name="message">The exception message.</param>
    public CommerzbankException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public CommerzbankException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with full HTTP response context.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code returned by the gateway.</param>
    /// <param name="correlationId">The value of the `X-CorrelationID` response header.</param>
    /// <param name="rawResponse">The raw response body.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public CommerzbankException(string message, HttpStatusCode? statusCode, string? correlationId, string? rawResponse, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        RawResponse = rawResponse;
    }
}
