using Banking.NET.Commerzbank.Exceptions;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Thrown when an ISO 20022 document fails structural validation, e.g. an unexpected root element while reading, or a writer constraint violation.</summary>
public sealed class Iso20022ValidationException : CommerzbankException
{
    /// <summary>The XML element path the validation failure relates to, if applicable.</summary>
    public string? Path { get; }

    /// <summary>Initializes a new instance with a message only.</summary>
    /// <param name="message">The exception message.</param>
    public Iso20022ValidationException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and the element path the failure relates to.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="path">The XML element path the validation failure relates to.</param>
    public Iso20022ValidationException(string message, string? path) : base(message)
    {
        Path = path;
    }
}
