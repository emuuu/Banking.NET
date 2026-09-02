using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Commerzbank.NET.CorporatePayments.Internal;
using Commerzbank.NET.Exceptions;
using Commerzbank.NET.Internal;

namespace Commerzbank.NET.CorporatePayments;

/// <summary>Default <see cref="ICorporatePaymentsClient"/> implementation, backed by an <see cref="HttpClient"/> registered via <see cref="CorporatePaymentsBuilderExtensions.AddCorporatePayments"/>.</summary>
public sealed class CorporatePaymentsClient : ICorporatePaymentsClient
{
    private const int MaxFragments = 10_000;

    /// <summary>The relative path Corporate Payments API requests are made against, appended to the client's base address.</summary>
    public const string BasePath = "corporate-payments-api/1/v1/bulk-payments/";

    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="httpClient">The HTTP client used for requests. Must have <see cref="HttpClient.BaseAddress"/> configured.</param>
    /// <exception cref="ArgumentException"><paramref name="httpClient"/> has no <see cref="HttpClient.BaseAddress"/>.</exception>
    public CorporatePaymentsClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (httpClient.BaseAddress is null)
            throw new ArgumentException("The HTTP client must have a base address configured.", nameof(httpClient));

        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task HeartbeatAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BasePath + "heartbeat", cancellationToken).ConfigureAwait(false);
        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MessageInfo>> ListMessagesAsync(OrderType? orderType = null, CancellationToken cancellationToken = default)
    {
        var url = BasePath + "messages";
        if (orderType is { } value)
            url += "?OrderType=" + Uri.EscapeDataString(value.Code);

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NoContent)
            return [];

        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var apiMessages = JsonSerializer.Deserialize(json, CommerzbankJsonContext.Default.ListApiMessageInfo);
        if (apiMessages is null || apiMessages.Count == 0)
            return [];

        return apiMessages.ConvertAll(apiMessage => new MessageInfo(apiMessage.MessageId, OrderType.Parse(apiMessage.OrderType), apiMessage.Fragments, apiMessage.Size));
    }

    /// <inheritdoc />
    public async Task<CorporatePaymentsMessage> DownloadMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        using var content = new MemoryStream();
        var (fragments, contentType) = await DownloadIntoAsync(messageId, null, content, cancellationToken).ConfigureAwait(false);

        return new CorporatePaymentsMessage
        {
            MessageId = messageId,
            Content = content.ToArray(),
            ContentType = contentType,
            FragmentCount = fragments,
        };
    }

    /// <inheritdoc />
    public async Task<CorporatePaymentsMessage> DownloadMessageAsync(MessageInfo message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var content = new MemoryStream();
        var (fragments, contentType) = await DownloadIntoAsync(message.MessageId, message.Fragments, content, cancellationToken).ConfigureAwait(false);

        return new CorporatePaymentsMessage
        {
            MessageId = message.MessageId,
            OrderType = message.OrderType,
            Content = content.ToArray(),
            ContentType = contentType,
            FragmentCount = fragments,
        };
    }

    /// <inheritdoc />
    public async Task<int> DownloadMessageAsync(string messageId, Stream destination, int? fragmentCount = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentNullException.ThrowIfNull(destination);

        var (fragments, _) = await DownloadIntoAsync(messageId, fragmentCount, destination, cancellationToken).ConfigureAwait(false);
        return fragments;
    }

    /// <inheritdoc />
    public async Task<MessageFragment> DownloadFragmentAsync(string messageId, int fragment, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentOutOfRangeException.ThrowIfNegative(fragment);

        var (status, content, contentType, correlationId) = await GetFragmentRawAsync(messageId, fragment, cancellationToken).ConfigureAwait(false);

        return status switch
        {
            HttpStatusCode.OK => new MessageFragment(fragment, content, contentType, IsPartial: false),
            HttpStatusCode.PartialContent => new MessageFragment(fragment, content, contentType, IsPartial: true),
            HttpStatusCode.RequestedRangeNotSatisfiable => throw new CommerzbankApiException($"Commerzbank API has no fragment {fragment} for message '{messageId}'.", status, correlationId, RawResponseOrNull(content)),
            _ => throw new CommerzbankApiException($"Commerzbank API returned an unexpected status {(int)status} for fragment {fragment} of message '{messageId}'.", status, correlationId, RawResponseOrNull(content)),
        };
    }

    /// <inheritdoc />
    public async Task ConfirmMessageAsync(string messageId, ReceivedStatus status = ReceivedStatus.Complete, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var received = status == ReceivedStatus.Complete ? "complete" : "partial";
        var json = JsonSerializer.Serialize(new ApiConfirmRequest(received), CommerzbankJsonContext.Default.ApiConfirmRequest);

        using var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync(MessageUrl(messageId), requestContent, cancellationToken).ConfigureAwait(false);
        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, Stream content, bool compress = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ValidateSubmittableOrderType(orderType);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return await SubmitOrderCoreAsync(orderType, buffer.ToArray(), compress, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, byte[] content, bool compress = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        return SubmitOrderCoreAsync(orderType, content, compress, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, string xml, bool compress = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        return SubmitOrderCoreAsync(orderType, Encoding.UTF8.GetBytes(xml), compress, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CorporatePaymentsMessage> FetchMessagesAsync(OrderType? orderType = null, bool confirm = true, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var infos = await ListMessagesAsync(orderType, cancellationToken).ConfigureAwait(false);
        foreach (var info in infos)
        {
            var message = await DownloadMessageAsync(info, cancellationToken).ConfigureAwait(false);
            yield return message;

            if (!confirm)
                continue;

            try
            {
                await ConfirmMessageAsync(info.MessageId, ReceivedStatus.Complete, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new MessageConfirmationException(info.MessageId, message, ex);
            }
        }
    }

    /// <summary>
    /// Downloads every fragment of a message into <paramref name="destination"/>, robust against the three ways
    /// the gateway may split a message: everything in one response, a 413 signalling the message must be
    /// requested fragment by fragment, or an explicit sequence of 206 responses terminated by either a final 200
    /// or a 416 once the last known fragment has been consumed. The safety cap of <see cref="MaxFragments"/> only
    /// triggers when no known fragment count terminated the loop first, so a message whose known fragment count
    /// equals the cap is read in full.
    /// </summary>
    private async Task<(int Fragments, string? ContentType)> DownloadIntoAsync(string messageId, int? fragmentCount, Stream destination, CancellationToken cancellationToken)
    {
        string? contentType = null;
        var written = 0;
        var nextIndex = 0;

        if (fragmentCount is null or 1)
        {
            var (status, responseContentType, correlationId, rawResponse) = await DownloadFragmentIntoAsync(messageId, null, destination, cancellationToken).ConfigureAwait(false);
            switch (status)
            {
                case HttpStatusCode.OK:
                    return (1, responseContentType);
                case HttpStatusCode.PartialContent:
                    contentType = responseContentType;
                    written = 1;
                    nextIndex = 1;
                    break;
                case HttpStatusCode.RequestEntityTooLarge:
                    nextIndex = 0;
                    break;
                default:
                    throw new CommerzbankApiException($"Commerzbank API returned an unexpected status {(int)status} for message '{messageId}'.", status, correlationId, rawResponse);
            }
        }

        HttpStatusCode? lastStatus = null;
        string? lastCorrelationId = null;
        string? lastRawResponse = null;

        for (var i = nextIndex; ; i++)
        {
            if (fragmentCount is { } limit && i >= limit)
                return (written, contentType);

            if (i >= MaxFragments)
                throw new CommerzbankApiException($"Message '{messageId}' exceeded the maximum of {MaxFragments} fragments.", lastStatus, lastCorrelationId, lastRawResponse);

            var (status, responseContentType, correlationId, rawResponse) = await DownloadFragmentIntoAsync(messageId, i, destination, cancellationToken).ConfigureAwait(false);
            lastStatus = status;
            lastCorrelationId = correlationId;
            lastRawResponse = rawResponse;

            switch (status)
            {
                case HttpStatusCode.OK:
                    if (written == 0)
                        contentType = responseContentType;
                    written++;
                    return (written, contentType);
                case HttpStatusCode.PartialContent:
                    if (written == 0)
                        contentType = responseContentType;
                    written++;
                    break;
                case HttpStatusCode.RequestedRangeNotSatisfiable when i > 0:
                    return (written, contentType);
                default:
                    throw new CommerzbankApiException($"Commerzbank API returned an unexpected status {(int)status} for fragment {i} of message '{messageId}'.", status, correlationId, rawResponse);
            }
        }
    }

    /// <summary>
    /// Requests a single fragment (or the whole message when <paramref name="fragment"/> is null) using
    /// <see cref="HttpCompletionOption.ResponseHeadersRead"/> and streams its body directly into
    /// <paramref name="destination"/> via <see cref="HttpContent.CopyToAsync(Stream, CancellationToken)"/> instead
    /// of buffering it as a byte array. Returns the raw status, content type, correlation ID and (for the
    /// non-content statuses 413/416) the response body — every other status is thrown via
    /// <see cref="CommerzbankErrorHandler"/>.
    /// </summary>
    private async Task<(HttpStatusCode Status, string? ContentType, string? CorrelationId, string? RawResponse)> DownloadFragmentIntoAsync(string messageId, int? fragment, Stream destination, CancellationToken cancellationToken)
    {
        var url = MessageUrl(messageId);
        if (fragment is { } index)
            url += "?Fragment=" + index.ToString(CultureInfo.InvariantCulture);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.PartialContent)
        {
            await response.Content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            return (response.StatusCode, response.Content.Headers.ContentType?.ToString(), CommerzbankErrorHandler.GetCorrelationId(response), null);
        }

        if (response.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (response.StatusCode, response.Content.Headers.ContentType?.ToString(), CommerzbankErrorHandler.GetCorrelationId(response), string.IsNullOrEmpty(body) ? null : body);
        }

        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new CommerzbankApiException($"Commerzbank API returned an unexpected successful status {(int)response.StatusCode} for message '{messageId}'.", response.StatusCode, CommerzbankErrorHandler.GetCorrelationId(response), string.IsNullOrEmpty(rawResponse) ? null : rawResponse);
    }

    /// <summary>Requests a single fragment (or the whole message when <paramref name="fragment"/> is null) and buffers its body as a byte array, returning the raw status, content, content type and correlation ID without any error mapping for 200, 206, 413 or 416 — every other status is thrown via <see cref="CommerzbankErrorHandler"/>.</summary>
    private async Task<(HttpStatusCode Status, byte[] Content, string? ContentType, string? CorrelationId)> GetFragmentRawAsync(string messageId, int? fragment, CancellationToken cancellationToken)
    {
        var url = MessageUrl(messageId);
        if (fragment is { } index)
            url += "?Fragment=" + index.ToString(CultureInfo.InvariantCulture);

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.PartialContent or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return (response.StatusCode, content, response.Content.Headers.ContentType?.ToString(), CommerzbankErrorHandler.GetCorrelationId(response));
        }

        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        throw new CommerzbankApiException($"Commerzbank API returned an unexpected successful status {(int)response.StatusCode} for message '{messageId}'.", response.StatusCode, CommerzbankErrorHandler.GetCorrelationId(response), null);
    }

    private static void ValidateSubmittableOrderType(OrderType orderType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderType.Code);
        if (orderType.Direction == OrderDirection.Download)
            throw new ArgumentException($"Order type '{orderType.Code}' is a download order type and cannot be submitted.", nameof(orderType));
    }

    private async Task<OrderSubmissionResult> SubmitOrderCoreAsync(OrderType orderType, byte[] content, bool compress, CancellationToken cancellationToken)
    {
        ValidateSubmittableOrderType(orderType);

        string mediaType;
        byte[] payload;
        if (IsGzip(content))
        {
            mediaType = "application/gzip";
            payload = content;
        }
        else if (compress)
        {
            mediaType = "application/gzip";
            payload = Compress(content);
        }
        else
        {
            mediaType = "application/xml";
            payload = content;
        }

        using var httpContent = new ByteArrayContent(payload);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

        using var request = new HttpRequestMessage(HttpMethod.Post, BasePath + "messages") { Content = httpContent };
        request.Headers.TryAddWithoutValidation("OrderType", orderType.Code);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await CommerzbankErrorHandler.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new OrderSubmissionResult(response.StatusCode, response.Headers.Location, string.IsNullOrEmpty(rawResponse) ? null : rawResponse);
    }

    private static bool IsGzip(byte[] content) => content.Length >= 2 && content[0] == 0x1F && content[1] == 0x8B;

    private static string? RawResponseOrNull(byte[] content) => content.Length == 0 ? null : Encoding.UTF8.GetString(content);

    private static byte[] Compress(byte[] content)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(content);
        return compressed.ToArray();
    }

    private static string MessageUrl(string messageId) => BasePath + "messages/" + Uri.EscapeDataString(messageId);
}
