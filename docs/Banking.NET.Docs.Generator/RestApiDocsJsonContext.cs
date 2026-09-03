using System.Text.Json.Serialization;

namespace Banking.NET.Docs.Generator;

/// <summary>Source-generated serializer for <see cref="RestApiDocsRoot"/>, avoiding reflection-based JSON when writing <c>rest-api-docs.json</c>.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RestApiDocsRoot))]
internal sealed partial class RestApiDocsJsonContext : JsonSerializerContext
{
}
