using System.Text.Json.Serialization;
using Commerzbank.NET.Auth;
using Commerzbank.NET.CorporatePayments.Internal;

namespace Commerzbank.NET.Internal;

[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(OAuthErrorResponse))]
[JsonSerializable(typeof(List<ApiMessageInfo>))]
[JsonSerializable(typeof(ApiConfirmRequest))]
internal sealed partial class CommerzbankJsonContext : JsonSerializerContext
{
}
