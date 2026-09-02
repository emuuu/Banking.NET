using System.Text.Json.Serialization;

namespace Commerzbank.NET.Auth;

internal sealed record OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
