using Commerzbank.NET.Exceptions;

namespace Commerzbank.NET.CorporatePayments;

/// <summary>A client for the Commerzbank Corporate Payments API: listing, downloading and confirming bank-generated messages, and submitting customer orders.</summary>
public interface ICorporatePaymentsClient
{
    /// <summary>Calls the API heartbeat endpoint to verify connectivity and authentication.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task HeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the messages currently waiting to be downloaded.</summary>
    /// <param name="orderType">Restricts the result to a single order type, or null to list all order types.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The waiting messages, or an empty list if there are none.</returns>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task<IReadOnlyList<MessageInfo>> ListMessagesAsync(OrderType? orderType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a message by ID, transparently retrieving every fragment. The fragment count is not known in
    /// advance, so the gateway is probed to detect how it splits the message; use the
    /// <see cref="DownloadMessageAsync(MessageInfo, CancellationToken)"/> overload when the fragment count is
    /// already known from a prior <see cref="ListMessagesAsync"/> call.
    /// </summary>
    /// <param name="messageId">The identifier of the message to download.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The downloaded message with all fragments concatenated.</returns>
    /// <exception cref="CommerzbankNotFoundException">No message with the given ID exists.</exception>
    /// <exception cref="CommerzbankGoneException">The message existed but is no longer available for download.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception, or the message exceeded the maximum supported fragment count.</exception>
    Task<CorporatePaymentsMessage> DownloadMessageAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>Downloads a message previously returned by <see cref="ListMessagesAsync"/>, using its known fragment count and order type.</summary>
    /// <param name="message">The message to download.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The downloaded message with all fragments concatenated.</returns>
    /// <exception cref="CommerzbankNotFoundException">No message with the given ID exists (any more).</exception>
    /// <exception cref="CommerzbankGoneException">The message existed but is no longer available for download.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception, or the message exceeded the maximum supported fragment count.</exception>
    Task<CorporatePaymentsMessage> DownloadMessageAsync(MessageInfo message, CancellationToken cancellationToken = default);

    /// <summary>Downloads a message by ID, streaming every fragment directly into <paramref name="destination"/> instead of buffering it in memory.</summary>
    /// <param name="messageId">The identifier of the message to download.</param>
    /// <param name="destination">The stream every fragment is written to, in order.</param>
    /// <param name="fragmentCount">The known number of fragments (e.g. from <see cref="MessageInfo.Fragments"/>), or null to probe the gateway.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of fragments that were downloaded.</returns>
    /// <exception cref="CommerzbankNotFoundException">No message with the given ID exists.</exception>
    /// <exception cref="CommerzbankGoneException">The message existed but is no longer available for download.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception, or the message exceeded the maximum supported fragment count.</exception>
    Task<int> DownloadMessageAsync(string messageId, Stream destination, int? fragmentCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a single, explicit fragment of a message. This is the low-level building block behind
    /// <see cref="DownloadMessageAsync(string, CancellationToken)"/>; most callers should use that method instead.
    /// </summary>
    /// <param name="messageId">The identifier of the message.</param>
    /// <param name="fragment">The zero-based fragment index.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested fragment. <see cref="MessageFragment.IsPartial"/> is true when the gateway answered with HTTP 206 (more fragments follow).</returns>
    /// <exception cref="CommerzbankNotFoundException">No message with the given ID exists.</exception>
    /// <exception cref="CommerzbankGoneException">The message existed but is no longer available for download.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception, including HTTP 416 for a fragment index the gateway cannot deliver.</exception>
    Task<MessageFragment> DownloadFragmentAsync(string messageId, int fragment, CancellationToken cancellationToken = default);

    /// <summary>Confirms receipt of a message so the bank stops redelivering it.</summary>
    /// <param name="messageId">The identifier of the message to confirm.</param>
    /// <param name="status">Whether the message was received in full or only partially. Defaults to <see cref="ReceivedStatus.Complete"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="CommerzbankNotFoundException">No message with the given ID exists.</exception>
    /// <exception cref="CommerzbankBadRequestException">The confirmation request was rejected as invalid.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task ConfirmMessageAsync(string messageId, ReceivedStatus status = ReceivedStatus.Complete, CancellationToken cancellationToken = default);

    /// <summary>Submits an order from a stream.</summary>
    /// <param name="orderType">The order type to submit; must be an upload order type.</param>
    /// <param name="content">The order content (a pain.001 or pain.008 document).</param>
    /// <param name="compress">Whether to gzip-compress the content before submitting it. Defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The submission result, including the gateway-assigned location of the order.</returns>
    /// <exception cref="ArgumentException"><paramref name="orderType"/> is a download order type.</exception>
    /// <exception cref="CommerzbankBadRequestException">The gateway rejected the submitted content, e.g. an invalid `OrderType`.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, Stream content, bool compress = false, CancellationToken cancellationToken = default);

    /// <summary>Submits an order from a byte array. If the bytes already start with the gzip magic bytes 0x1F 0x8B, they are submitted as-is with an `application/gzip` content type, regardless of <paramref name="compress"/>.</summary>
    /// <param name="orderType">The order type to submit; must be an upload order type.</param>
    /// <param name="content">The order content (a pain.001 or pain.008 document).</param>
    /// <param name="compress">Whether to gzip-compress the content before submitting it. Defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The submission result, including the gateway-assigned location of the order.</returns>
    /// <exception cref="ArgumentException"><paramref name="orderType"/> is a download order type.</exception>
    /// <exception cref="CommerzbankBadRequestException">The gateway rejected the submitted content, e.g. an invalid `OrderType`.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, byte[] content, bool compress = false, CancellationToken cancellationToken = default);

    /// <summary>Submits an order from an XML string, encoded as UTF-8 without a byte order mark.</summary>
    /// <param name="orderType">The order type to submit; must be an upload order type.</param>
    /// <param name="xml">The order content (a pain.001 or pain.008 document) as XML text.</param>
    /// <param name="compress">Whether to gzip-compress the content before submitting it. Defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The submission result, including the gateway-assigned location of the order.</returns>
    /// <exception cref="ArgumentException"><paramref name="orderType"/> is a download order type.</exception>
    /// <exception cref="CommerzbankBadRequestException">The gateway rejected the submitted content, e.g. an invalid `OrderType`.</exception>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected the request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, string xml, bool compress = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists and downloads waiting messages one at a time, confirming each one after it has been yielded to the
    /// caller. Confirmation of a message happens on the *next* call to <c>MoveNextAsync</c>, i.e. after the
    /// consumer has finished processing the previously yielded message — this gives the at-least-once semantics
    /// described by <see cref="MessageConfirmationException"/>: if the enumeration is abandoned (e.g. via
    /// <c>break</c>) or the consumer throws while processing a message, that message is left unconfirmed and the
    /// bank keeps redelivering it on the next call. If confirmation itself fails, a
    /// <see cref="MessageConfirmationException"/> is thrown from the following <c>MoveNextAsync</c>, carrying both
    /// the message ID and the already-downloaded message so the caller does not lose it.
    /// </summary>
    /// <param name="orderType">Restricts the result to a single order type, or null to fetch all order types.</param>
    /// <param name="confirm">Whether to confirm each message as <see cref="ReceivedStatus.Complete"/> after it is yielded. Defaults to true.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous sequence of downloaded messages.</returns>
    /// <exception cref="CommerzbankAuthenticationException">The gateway rejected a request as unauthenticated.</exception>
    /// <exception cref="CommerzbankApiException">The gateway returned an unsuccessful status not covered by a more specific exception.</exception>
    /// <exception cref="MessageConfirmationException">A message was downloaded but confirming it failed.</exception>
    IAsyncEnumerable<CorporatePaymentsMessage> FetchMessagesAsync(OrderType? orderType = null, bool confirm = true, CancellationToken cancellationToken = default);
}
