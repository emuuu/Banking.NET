using System.Net.Http.Json;
using Commerzbank.NET.Docs.Models;

namespace Commerzbank.NET.Docs.Services;

public class RestApiDocService : IRestApiDocService
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private RestApiDocsRoot? _docs;

    public RestApiDocService(HttpClient http)
    {
        _http = http;
    }

    public IReadOnlyList<string> GroupOrder { get; } =
        ["Configuration", "Authentication", "Exceptions", "Corporate Payments", "ISO 20022"];

    public async Task InitializeAsync()
    {
        if (_docs is not null) return;
        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_docs is not null) return;
            _docs = await _http.GetFromJsonAsync<RestApiDocsRoot>("data/rest-api-docs.json").ConfigureAwait(false);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public List<TypeDoc> GetAllTypes() => _docs?.Types ?? [];

    public List<TypeDoc> GetTypesByGroup(string group) =>
        GetAllTypes().Where(t => t.Group == group).ToList();

    public TypeDoc? GetType(string name) =>
        GetAllTypes().FirstOrDefault(t => t.Name == name);

    public List<EnumDoc> GetAllEnums() => _docs?.Enums ?? [];

    public List<EnumDoc> GetEnumsByGroup(string group) =>
        GetAllEnums().Where(e => e.Group == group).ToList();

    public EnumDoc? GetEnum(string name) =>
        GetAllEnums().FirstOrDefault(e => e.Name == name);
}
