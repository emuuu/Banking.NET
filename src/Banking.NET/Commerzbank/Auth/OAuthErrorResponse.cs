using System.Text.Json.Serialization;

namespace Banking.NET.Commerzbank.Auth;

internal sealed record OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
