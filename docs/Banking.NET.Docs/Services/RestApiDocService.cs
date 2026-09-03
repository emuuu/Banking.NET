using System.Net.Http.Json;
using Banking.NET.Docs.Models;

namespace Banking.NET.Docs.Services;

/// <summary>Default <see cref="IRestApiDocService"/> implementation, backed by the generated JSON file.</summary>
public class RestApiDocService : IRestApiDocService
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private RestApiDocsRoot? _docs;

    /// <param name="http">The HTTP client the generated <c>data/rest-api-docs.json</c> is fetched with.</param>
    public RestApiDocService(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GroupOrder { get; } =
        ["Configuration", "Authentication", "Exceptions", "Corporate Payments", "ISO 20022"];

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_docs is not null) return;
        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_docs is not null) return;
            _docs = await _http.GetFromJsonAsync("data/rest-api-docs.json", DocsJsonContext.Default.RestApiDocsRoot).ConfigureAwait(false);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public List<TypeDoc> GetAllTypes() => _docs?.Types ?? [];

    /// <inheritdoc />
    public List<TypeDoc> GetTypesByGroup(string group) =>
        GetAllTypes().Where(t => t.Group == group).ToList();

    /// <inheritdoc />
    public TypeDoc? GetType(string slug) =>
        GetAllTypes().FirstOrDefault(t => t.Slug == slug);

    /// <inheritdoc />
    public List<EnumDoc> GetAllEnums() => _docs?.Enums ?? [];

    /// <inheritdoc />
    public List<EnumDoc> GetEnumsByGroup(string group) =>
        GetAllEnums().Where(e => e.Group == group).ToList();

    /// <inheritdoc />
    public EnumDoc? GetEnum(string slug) =>
        GetAllEnums().FirstOrDefault(e => e.Slug == slug);
}
