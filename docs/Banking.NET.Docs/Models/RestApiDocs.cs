namespace Banking.NET.Docs.Models;

public class RestApiDocsRoot
{
    public List<TypeDoc> Types { get; set; } = [];
    public List<EnumDoc> Enums { get; set; } = [];
}

public class TypeDoc
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Group { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ConstructorDoc> Constructors { get; set; } = [];
    public List<PropertyDoc> Properties { get; set; } = [];
    public List<MethodDoc> Methods { get; set; } = [];
}

public class ConstructorDoc
{
    public string Description { get; set; } = "";
    public List<ParamDocEntry> Parameters { get; set; } = [];
}

public class MethodDoc
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ReturnType { get; set; } = "";
    public bool IsStatic { get; set; }
    public List<ParamDocEntry> Parameters { get; set; } = [];
}

public class PropertyDoc
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Accessors { get; set; } = "";
    public bool Required { get; set; }
}

public class ParamDocEntry
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Required { get; set; }
    public string Description { get; set; } = "";
    public string? Default { get; set; }
}

public class EnumDoc
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Group { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsFlags { get; set; }
    public List<EnumValueDoc> Values { get; set; } = [];
}

public class EnumValueDoc
{
    public string Name { get; set; } = "";
    public string SerializedValue { get; set; } = "";
}
