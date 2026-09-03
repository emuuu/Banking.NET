using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Commerzbank.NET;

namespace Commerzbank.NET.Docs.Generator;

/// <summary>Reflects over the Commerzbank.NET assembly and its XML documentation to produce the API reference data consumed by the docs site.</summary>
public class RestApiDocGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly (string Namespace, string Group)[] NamespaceGroups =
    [
        ("Commerzbank.NET.CorporatePayments.Iso20022", "ISO 20022"),
        ("Commerzbank.NET.CorporatePayments", "Corporate Payments"),
        ("Commerzbank.NET.Exceptions", "Exceptions"),
        ("Commerzbank.NET.Auth", "Authentication"),
        ("Commerzbank.NET", "Configuration"),
    ];

    private Dictionary<string, string> _xmlDocs = new();
    private readonly NullabilityInfoContext _nullabilityCtx = new();

    public async Task<RestApiDocsRoot> GenerateAsync(string outputPath)
    {
        var assembly = typeof(CommerzbankOptions).Assembly;
        var xmlPath = FindXmlDocPath(assembly);
        _xmlDocs = xmlPath is not null ? LoadXmlDocs(xmlPath) : new Dictionary<string, string>();

        var allTypes = assembly.GetExportedTypes()
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith("Commerzbank.NET", StringComparison.Ordinal))
            .OrderBy(t => t.Namespace, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var enums = allTypes
            .Where(t => t.IsEnum)
            .Select(BuildEnumDoc)
            .OrderBy(e => Array.IndexOf(GroupOrder.Values, e.Group))
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToList();

        var types = allTypes
            .Where(t => !t.IsEnum)
            .Select(BuildTypeDoc)
            .OrderBy(t => Array.IndexOf(GroupOrder.Values, t.Group))
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var root = new RestApiDocsRoot { Types = types, Enums = enums };

        var json = JsonSerializer.Serialize(root, JsonOptions);
        await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
        Console.WriteLine($"  Generated {types.Count} types, {enums.Count} enums -> {Path.GetFileName(outputPath)}");
        return root;
    }

    private static class GroupOrder
    {
        public static readonly string[] Values = ["Configuration", "Authentication", "Exceptions", "Corporate Payments", "ISO 20022"];
    }

    // ── Type building ──

    private TypeDoc BuildTypeDoc(Type type)
    {
        var kind = DetermineKind(type);

        return new TypeDoc
        {
            Name = type.Name,
            Namespace = type.Namespace ?? "",
            Group = GroupFor(type.Namespace ?? ""),
            Kind = kind,
            Description = GetXmlSummary(type),
            Constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(BuildConstructorDoc)
                .ToList(),
            Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(BuildPropertyDoc)
                .ToList(),
            Methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => IsDocumentableMethod(m, kind))
                .Select(BuildMethodDoc)
                .ToList()
        };
    }

    private static string DetermineKind(Type type)
    {
        if (type.IsInterface) return "Interface";
        if (type.IsClass && type.IsAbstract && type.IsSealed) return "Static class";

        if (type.IsValueType)
            return IsRecordLike(type) ? "Record struct" : "Struct";

        return IsRecordLike(type) ? "Record" : "Class";
    }

    private static bool IsRecordLike(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.Name is "<Clone>$" or "Deconstruct");

    private static string GroupFor(string ns) =>
        NamespaceGroups.First(g => ns.StartsWith(g.Namespace, StringComparison.Ordinal)).Group;

    private static bool IsDocumentableMethod(MethodInfo method, string kind)
    {
        if (method.IsSpecialName) return false;
        if (method.Name.Contains('<', StringComparison.Ordinal)) return false;
        if (method.Name is "PrintMembers") return false;

        if (kind.StartsWith("Record", StringComparison.Ordinal) && method.Name is "Equals" or "GetHashCode" or "ToString" or "Deconstruct")
            return false;

        return true;
    }

    private ConstructorDoc BuildConstructorDoc(ConstructorInfo ctor) => new()
    {
        Description = GetXmlMemberSummary(ctor.DeclaringType!, "#ctor", "M"),
        Parameters = ctor.GetParameters().Select(p => BuildParamDoc(p, ctor.DeclaringType!, "#ctor")).ToList()
    };

    private MethodDoc BuildMethodDoc(MethodInfo method) => new()
    {
        Name = method.Name,
        Description = GetXmlMemberSummary(method.DeclaringType!, method.Name, "M"),
        ReturnType = FormatReturnType(method.ReturnType),
        IsStatic = method.IsStatic,
        Parameters = method.GetParameters()
            .Where(p => p.ParameterType != typeof(CancellationToken))
            .Select(p => BuildParamDoc(p, method.DeclaringType!, method.Name))
            .ToList()
    };

    private ParamDocEntry BuildParamDoc(ParameterInfo param, Type declaringType, string methodName)
    {
        var parameterType = param.ParameterType;
        var refModifier = "";
        if (parameterType.IsByRef)
        {
            parameterType = parameterType.GetElementType()!;
            refModifier = param.IsOut ? "out " : "ref ";
        }

        var isNullable = IsNullableParameter(param);
        var typeName = FormatTypeName(parameterType);
        return new ParamDocEntry
        {
            Name = param.Name ?? "",
            Type = refModifier + typeName + (isNullable && !typeName.EndsWith('?') ? "?" : ""),
            Required = !param.HasDefaultValue && !isNullable,
            Description = GetXmlParamSummary(declaringType, methodName, param.Name ?? ""),
            Default = param.HasDefaultValue ? FormatDefaultValue(param.DefaultValue) : null
        };
    }

    private PropertyDoc BuildPropertyDoc(PropertyInfo prop) => new()
    {
        Name = prop.Name,
        Type = FormatPropertyTypeName(prop),
        Description = GetXmlMemberSummary(prop.DeclaringType!, prop.Name, "P"),
        Accessors = DescribeAccessors(prop),
        Required = IsRequiredMember(prop)
    };

    private static string DescribeAccessors(PropertyInfo prop)
    {
        var parts = new List<string>();
        if (prop.GetMethod is not null) parts.Add("get");
        if (prop.SetMethod is not null)
        {
            var isInit = prop.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            parts.Add(isInit ? "init" : "set");
        }
        return string.Join(", ", parts);
    }

    private static bool IsRequiredMember(PropertyInfo prop) =>
        prop.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");

    private EnumDoc BuildEnumDoc(Type enumType)
    {
        var values = new List<EnumValueDoc>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var field = enumType.GetField(name)!;
            var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
            var rawValue = Convert.ToInt64(Enum.Parse(enumType, name));
            values.Add(new EnumValueDoc
            {
                Name = name,
                SerializedValue = enumMember?.Value ?? rawValue.ToString()
            });
        }

        return new EnumDoc
        {
            Name = enumType.Name,
            Namespace = enumType.Namespace ?? "",
            Group = GroupFor(enumType.Namespace ?? ""),
            Description = GetXmlSummary(enumType),
            IsFlags = enumType.GetCustomAttribute<FlagsAttribute>() is not null,
            Values = values
        };
    }

    // ── XML doc helpers ──

    private string GetXmlSummary(Type type)
    {
        var key = $"T:{type.FullName}";
        return _xmlDocs.GetValueOrDefault(key, "");
    }

    private string GetXmlMemberSummary(Type type, string memberName, string prefix)
    {
        var key = $"{prefix}:{type.FullName}.{memberName}";
        if (_xmlDocs.TryGetValue(key, out var doc))
            return doc;

        // Methods/constructors carry a parenthesized parameter list in their XML doc key; matching on
        // "key(" avoids false positives between methods whose names share a prefix (e.g. Read vs ReadAsync).
        var keyWithParen = key + "(";
        foreach (var kvp in _xmlDocs)
        {
            if (kvp.Key.StartsWith(keyWithParen, StringComparison.Ordinal))
                return kvp.Value;
        }

        return "";
    }

    private string GetXmlParamSummary(Type type, string methodName, string paramName)
    {
        var methodPrefix = $"M:{type.FullName}.{methodName}(";
        var paramMarker = $"|param:{paramName}";
        foreach (var kvp in _xmlDocs)
        {
            if (kvp.Key.StartsWith(methodPrefix, StringComparison.Ordinal) && kvp.Key.EndsWith(paramMarker, StringComparison.Ordinal))
                return kvp.Value;
        }

        return "";
    }

    // ── Type formatting ──

    private static string FormatTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(object)) return "object";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(DateOnly)) return "DateOnly";
        if (type == typeof(DateTimeOffset)) return "DateTimeOffset";
        if (type == typeof(TimeSpan)) return "TimeSpan";
        if (type == typeof(Uri)) return "Uri";
        if (type == typeof(byte[])) return "byte[]";
        if (type == typeof(Stream)) return "Stream";
        if (type == typeof(Task)) return "Task";
        if (type == typeof(ValueTask)) return "ValueTask";
        if (type == typeof(CancellationToken)) return "CancellationToken";

        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying is not null)
            return $"{FormatTypeName(nullableUnderlying)}?";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{name}<{args}>";
        }

        return type.Name;
    }

    private string FormatPropertyTypeName(PropertyInfo prop)
    {
        var typeName = FormatTypeName(prop.PropertyType);
        if (!typeName.EndsWith('?') && IsNullableProperty(prop))
            typeName += "?";
        return typeName;
    }

    private static string FormatReturnType(Type type)
    {
        var inner = UnwrapAsyncType(type);
        if (inner is null) return FormatTypeName(type);
        if (inner == typeof(void)) return type == typeof(Task) ? "Task" : "ValueTask";
        return FormatTypeName(inner);
    }

    private static string? FormatDefaultValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            double d => d.ToString("G"),
            Enum e => e.ToString(),
            _ => value.ToString()
        };
    }

    // ── Nullability helpers ──

    private bool IsNullableProperty(PropertyInfo prop)
    {
        if (Nullable.GetUnderlyingType(prop.PropertyType) is not null)
            return true;

        try
        {
            var info = _nullabilityCtx.Create(prop);
            return info.ReadState == NullabilityState.Nullable;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private bool IsNullableParameter(ParameterInfo param)
    {
        if (Nullable.GetUnderlyingType(param.ParameterType) is not null)
            return true;

        if (param.HasDefaultValue && param.DefaultValue is null)
            return true;

        try
        {
            var info = _nullabilityCtx.Create(param);
            return info.ReadState == NullabilityState.Nullable;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // ── Async return type helpers ──

    private static Type? UnwrapAsyncType(Type type)
    {
        if (type == typeof(Task) || type == typeof(ValueTask))
            return typeof(void);

        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(Task<>) || genDef == typeof(ValueTask<>))
                return type.GetGenericArguments()[0];
        }

        return null;
    }

    // ── XML doc loading ──

    private static string? FindXmlDocPath(Assembly assembly)
    {
        var dllPath = assembly.Location;
        if (string.IsNullOrEmpty(dllPath)) return null;
        var xmlPath = Path.ChangeExtension(dllPath, ".xml");
        return File.Exists(xmlPath) ? xmlPath : null;
    }

    /// <summary>
    /// Loads XML doc summaries. Parameter descriptions are stored under a synthetic key
    /// <c>M:Type.Method|param:name</c> so <see cref="GetXmlParamSummary"/> can look them up without a second dictionary.
    /// </summary>
    private static Dictionary<string, string> LoadXmlDocs(string xmlPath)
    {
        var summaries = new Dictionary<string, string>();
        try
        {
            var doc = XDocument.Load(xmlPath);
            foreach (var member in doc.Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;
                if (name is null) continue;

                var summary = member.Element("summary")?.Value.Trim();
                if (!string.IsNullOrEmpty(summary))
                {
                    summary = string.Join(" ", summary.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
                    summaries[name] = summary;
                }

                foreach (var paramEl in member.Elements("param"))
                {
                    var paramName = paramEl.Attribute("name")?.Value;
                    var paramDesc = paramEl.Value.Trim();
                    if (paramName is not null && !string.IsNullOrEmpty(paramDesc))
                    {
                        paramDesc = string.Join(" ", paramDesc.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
                        summaries[$"{name}|param:{paramName}"] = paramDesc;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or System.Xml.XmlException)
        {
            Console.Error.WriteLine($"Warning: Could not parse XML docs: {ex.Message}");
        }
        return summaries;
    }
}

// ── Output models ──

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
