using Banking.NET.Docs.Models;

namespace Banking.NET.Docs.Services;

/// <summary>Loads and queries the generated API reference data (<c>data/rest-api-docs.json</c>).</summary>
public interface IRestApiDocService
{
    /// <summary>Loads the API reference data on first call; subsequent calls are no-ops.</summary>
    Task InitializeAsync();

    /// <summary>The API reference groups, in display order.</summary>
    IReadOnlyList<string> GroupOrder { get; }

    /// <summary>Returns every documented type.</summary>
    List<TypeDoc> GetAllTypes();

    /// <summary>Returns the documented types belonging to <paramref name="group"/>.</summary>
    List<TypeDoc> GetTypesByGroup(string group);

    /// <summary>Returns the type with the given <see cref="TypeDoc.Slug"/>, or null if none matches.</summary>
    TypeDoc? GetType(string slug);

    /// <summary>Returns every documented enum.</summary>
    List<EnumDoc> GetAllEnums();

    /// <summary>Returns the documented enums belonging to <paramref name="group"/>.</summary>
    List<EnumDoc> GetEnumsByGroup(string group);

    /// <summary>Returns the enum with the given <see cref="EnumDoc.Slug"/>, or null if none matches.</summary>
    EnumDoc? GetEnum(string slug);
}
