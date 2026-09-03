using Banking.NET.Docs.Models;

namespace Banking.NET.Docs.Services;

public interface IRestApiDocService
{
    Task InitializeAsync();
    IReadOnlyList<string> GroupOrder { get; }
    List<TypeDoc> GetAllTypes();
    List<TypeDoc> GetTypesByGroup(string group);
    TypeDoc? GetType(string name);
    List<EnumDoc> GetAllEnums();
    List<EnumDoc> GetEnumsByGroup(string group);
    EnumDoc? GetEnum(string name);
}
