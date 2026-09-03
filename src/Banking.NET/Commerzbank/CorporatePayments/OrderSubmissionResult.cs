using System.Net;

namespace Banking.NET.Commerzbank.CorporatePayments;

/// <summary>The result of <see cref="ICorporatePaymentsClient.SubmitOrderAsync(OrderType, Stream, bool, CancellationToken)"/> and its overloads.</summary>
/// <param name="StatusCode">The HTTP status code returned by the gateway, typically 201 Created.</param>
/// <param name="Location">The `Location` response header, if present. The sandbox returns 201 Created without this header, so it is regularly null there.</param>
/// <param name="RawResponse">The raw response body, if any. The sandbox returns 201 Created with an empty body, so it is regularly null there.</param>
public sealed record OrderSubmissionResult(HttpStatusCode StatusCode, Uri? Location, string? RawResponse);
