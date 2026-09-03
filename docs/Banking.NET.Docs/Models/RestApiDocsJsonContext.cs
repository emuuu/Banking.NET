using System.Text.Json.Serialization;

namespace Banking.NET.Docs.Models;

/// <summary>Source-generated serializer for <see cref="RestApiDocsRoot"/>, required for reading <c>data/rest-api-docs.json</c> reliably under Blazor WebAssembly's IL trimming.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RestApiDocsRoot))]
internal sealed partial class RestApiDocsJsonContext : JsonSerializerContext
{
}
