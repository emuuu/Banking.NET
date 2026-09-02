using System.Text.Json.Serialization;
using Commerzbank.NET.Auth;

namespace Commerzbank.NET.Internal;

[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(OAuthErrorResponse))]
internal sealed partial class CommerzbankJsonContext : JsonSerializerContext
{
}
