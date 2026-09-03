using System.Text.Json.Serialization;

namespace Banking.NET.Docs.Generator;

/// <summary>Source-generated serializer for <see cref="RestApiDocsRoot"/> and <see cref="ContentIndexEntry"/>, avoiding reflection-based JSON when writing <c>rest-api-docs.json</c> and <c>content-index.json</c>.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RestApiDocsRoot))]
[JsonSerializable(typeof(List<ContentIndexEntry>))]
internal sealed partial class DocsJsonContext : JsonSerializerContext
{
}
