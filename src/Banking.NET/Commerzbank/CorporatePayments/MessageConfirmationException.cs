using Banking.NET.Commerzbank.Exceptions;

namespace Banking.NET.Commerzbank.CorporatePayments;

/// <summary>Thrown by <see cref="ICorporatePaymentsClient.FetchMessagesAsync"/> when a message was downloaded but its confirmation failed. The bank keeps redelivering an unconfirmed message.</summary>
public sealed class MessageConfirmationException : CommerzbankException
{
    /// <summary>The identifier of the message that could not be confirmed.</summary>
    public string MessageId { get; }

    /// <summary>The message that was successfully downloaded before the confirmation failed.</summary>
    public CorporatePaymentsMessage DownloadedMessage { get; }

    /// <summary>Initializes a new instance. When <paramref name="innerException"/> is a <see cref="CommerzbankException"/>, its status code, correlation ID and raw response are carried over to this exception.</summary>
    /// <param name="messageId">The identifier of the message that could not be confirmed.</param>
    /// <param name="downloadedMessage">The message that was successfully downloaded before the confirmation failed.</param>
    /// <param name="innerException">The exception thrown while confirming the message.</param>
    public MessageConfirmationException(string messageId, CorporatePaymentsMessage downloadedMessage, Exception innerException)
        : base(
            $"Message '{messageId}' was downloaded but could not be confirmed. The bank keeps delivering it until it is confirmed.",
            (innerException as CommerzbankException)?.StatusCode,
            (innerException as CommerzbankException)?.CorrelationId,
            (innerException as CommerzbankException)?.RawResponse,
            innerException)
    {
        MessageId = messageId;
        DownloadedMessage = downloadedMessage;
    }
}
