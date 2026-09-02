using System.Text.Json.Serialization;

namespace Commerzbank.NET.CorporatePayments.Internal;

internal sealed record ApiConfirmRequest([property: JsonPropertyName("received")] string Received);
