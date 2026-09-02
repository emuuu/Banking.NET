using System.Net;

namespace Commerzbank.NET.CorporatePayments;

/// <summary>The result of <see cref="ICorporatePaymentsClient.SubmitOrderAsync(OrderType, Stream, bool, CancellationToken)"/> and its overloads.</summary>
/// <param name="StatusCode">The HTTP status code returned by the gateway, typically 201 Created.</param>
/// <param name="Location">The `Location` response header, if present.</param>
/// <param name="RawResponse">The raw response body, if any.</param>
public sealed record OrderSubmissionResult(HttpStatusCode StatusCode, Uri? Location, string? RawResponse);
