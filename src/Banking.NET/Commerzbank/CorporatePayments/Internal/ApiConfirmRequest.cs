using System.Text.Json.Serialization;

namespace Banking.NET.Commerzbank.CorporatePayments.Internal;

internal sealed record ApiConfirmRequest([property: JsonPropertyName("received")] string Received);
