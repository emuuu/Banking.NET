using System.Text.Json.Serialization;
using Banking.NET.Commerzbank.Auth;
using Banking.NET.Commerzbank.CorporatePayments.Internal;

namespace Banking.NET.Commerzbank.Internal;

[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(OAuthErrorResponse))]
[JsonSerializable(typeof(List<ApiMessageInfo>))]
[JsonSerializable(typeof(ApiConfirmRequest))]
internal sealed partial class CommerzbankJsonContext : JsonSerializerContext
{
}
