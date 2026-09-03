using System.Text.Json.Serialization;

namespace Banking.NET.Docs.Models;

/// <summary>Source-generated serializer for <see cref="RestApiDocsRoot"/> and <see cref="ContentIndex"/>, required for reading <c>data/rest-api-docs.json</c> and <c>data/content-index.json</c> reliably under Blazor WebAssembly's IL trimming.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RestApiDocsRoot))]
[JsonSerializable(typeof(ContentIndex[]))]
internal sealed partial class DocsJsonContext : JsonSerializerContext
{
}
