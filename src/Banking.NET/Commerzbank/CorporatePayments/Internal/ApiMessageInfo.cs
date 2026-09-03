using System.Text.Json.Serialization;

namespace Banking.NET.Commerzbank.CorporatePayments.Internal;

internal sealed record ApiMessageInfo
{
    [JsonPropertyName("MessageId")]
    public required string MessageId { get; init; }

    [JsonPropertyName("OrderType")]
    public required string OrderType { get; init; }

    [JsonPropertyName("Fragments")]
    public int Fragments { get; init; }

    [JsonPropertyName("Size")]
    public long Size { get; init; }
}
