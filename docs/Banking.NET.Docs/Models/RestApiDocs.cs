namespace Banking.NET.Docs.Models;

/// <summary>The complete generated API reference: every documented public type and enum.</summary>
public class RestApiDocsRoot
{
    /// <summary>The documented classes, interfaces, structs and records.</summary>
    public List<TypeDoc> Types { get; set; } = [];

    /// <summary>The documented enums.</summary>
    public List<EnumDoc> Enums { get; set; } = [];
}

/// <summary>API reference data for a single class, interface, struct or record.</summary>
public class TypeDoc
{
    /// <summary>The simple type name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The URL-safe identifier this type is reachable at under <c>api/{slug}</c>; equal to <see cref="Name"/> unless another type shares it.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The fully qualified namespace.</summary>
    public string Namespace { get; set; } = "";

    /// <summary>The API reference group this type is listed under (e.g. "ISO 20022").</summary>
    public string Group { get; set; } = "";

    /// <summary>The C# kind of the type (Class, Interface, Struct, Record, Record struct, or Static class).</summary>
    public string Kind { get; set; } = "";

    /// <summary>The type's XML doc summary.</summary>
    public string Description { get; set; } = "";

    /// <summary>The public instance constructors.</summary>
    public List<ConstructorDoc> Constructors { get; set; } = [];

    /// <summary>The public const fields.</summary>
    public List<FieldDoc> Fields { get; set; } = [];

    /// <summary>The public instance and static properties, including indexers.</summary>
    public List<PropertyDoc> Properties { get; set; } = [];

    /// <summary>The public instance and static methods, including operators.</summary>
    public List<MethodDoc> Methods { get; set; } = [];
}

/// <summary>API reference data for a public constructor.</summary>
public class ConstructorDoc
{
    /// <summary>The constructor's XML doc summary.</summary>
    public string Description { get; set; } = "";

    /// <summary>The constructor parameters.</summary>
    public List<ParamDocEntry> Parameters { get; set; } = [];
}

/// <summary>API reference data for a public method or operator.</summary>
public class MethodDoc
{
    /// <summary>The method name, or a symbolic name (e.g. "operator ==") for operator overloads.</summary>
    public string Name { get; set; } = "";

    /// <summary>The method's XML doc summary, resolved through <c>&lt;inheritdoc/&gt;</c> when needed.</summary>
    public string Description { get; set; } = "";

    /// <summary>The formatted return type, e.g. <c>Task&lt;OrderSubmissionResult&gt;</c>.</summary>
    public string ReturnType { get; set; } = "";

    /// <summary>Whether the method is static.</summary>
    public bool IsStatic { get; set; }

    /// <summary>The method parameters, including <c>CancellationToken</c> parameters.</summary>
    public List<ParamDocEntry> Parameters { get; set; } = [];
}

/// <summary>API reference data for a public property or indexer.</summary>
public class PropertyDoc
{
    /// <summary>The property name, or <c>this[...]</c> with its parameter list for an indexer.</summary>
    public string Name { get; set; } = "";

    /// <summary>The formatted property type.</summary>
    public string Type { get; set; } = "";

    /// <summary>The property's XML doc summary, resolved through <c>&lt;inheritdoc/&gt;</c> when needed.</summary>
    public string Description { get; set; } = "";

    /// <summary>The available accessors, e.g. "get, init".</summary>
    public string Accessors { get; set; } = "";

    /// <summary>Whether the property is a C# <c>required</c> member.</summary>
    public bool Required { get; set; }
}

/// <summary>API reference data for a public const field.</summary>
public class FieldDoc
{
    /// <summary>The field name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The formatted field type.</summary>
    public string Type { get; set; } = "";

    /// <summary>The constant's literal value, formatted like a C# expression.</summary>
    public string Value { get; set; } = "";

    /// <summary>The field's XML doc summary.</summary>
    public string Description { get; set; } = "";
}

/// <summary>API reference data for one constructor, method or indexer parameter.</summary>
public class ParamDocEntry
{
    /// <summary>The parameter name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The formatted parameter type, including a <c>ref</c>/<c>out</c> modifier and nullable suffix where applicable.</summary>
    public string Type { get; set; } = "";

    /// <summary>Whether the parameter has no default value and is not nullable.</summary>
    public bool Required { get; set; }

    /// <summary>The parameter's XML doc <c>&lt;param&gt;</c> text.</summary>
    public string Description { get; set; } = "";

    /// <summary>The formatted default value expression (e.g. <c>"false"</c>, <c>"null"</c>, <c>"default"</c>), or null if the parameter has none.</summary>
    public string? Default { get; set; }
}

/// <summary>API reference data for a public enum.</summary>
public class EnumDoc
{
    /// <summary>The simple enum name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The URL-safe identifier this enum is reachable at under <c>api/{slug}</c>; equal to <see cref="Name"/> unless another type shares it.</summary>
    public string Slug { get; set; } = "";

    /// <summary>The fully qualified namespace.</summary>
    public string Namespace { get; set; } = "";

    /// <summary>The API reference group this enum is listed under.</summary>
    public string Group { get; set; } = "";

    /// <summary>The enum's XML doc summary.</summary>
    public string Description { get; set; } = "";

    /// <summary>Whether the enum is decorated with <c>[Flags]</c>.</summary>
    public bool IsFlags { get; set; }

    /// <summary>The enum members.</summary>
    public List<EnumValueDoc> Values { get; set; } = [];
}

/// <summary>API reference data for a single enum member.</summary>
public class EnumValueDoc
{
    /// <summary>The member name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The wire value used when the member is JSON-serialized.</summary>
    public string SerializedValue { get; set; } = "";

    /// <summary>The member's XML doc summary.</summary>
    public string Description { get; set; } = "";
}
