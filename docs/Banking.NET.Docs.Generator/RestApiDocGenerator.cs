using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml.Linq;
using Banking.NET.Commerzbank;

namespace Banking.NET.Docs.Generator;

/// <summary>Reflects over the Banking.NET assembly and its XML documentation to produce the API reference data consumed by the docs site.</summary>
public class RestApiDocGenerator
{
    private static readonly (string Namespace, string Group)[] NamespaceGroups =
    [
        ("Banking.NET.Commerzbank.CorporatePayments.Iso20022", "ISO 20022"),
        ("Banking.NET.Commerzbank.CorporatePayments", "Corporate Payments"),
        ("Banking.NET.Commerzbank.Exceptions", "Exceptions"),
        ("Banking.NET.Commerzbank.Auth", "Authentication"),
        ("Banking.NET.Commerzbank", "Configuration"),
    ];

    private Dictionary<string, string> _xmlDocs = new();
    private readonly NullabilityInfoContext _nullabilityCtx = new();

    /// <summary>Reflects over the Banking.NET assembly, resolves XML documentation for every public member, and writes the result as JSON to <paramref name="outputPath"/>.</summary>
    /// <param name="outputPath">The file the generated JSON is written to.</param>
    /// <returns>The generated documentation root, for callers that also need the type/enum list (e.g. to build the sitemap).</returns>
    public async Task<RestApiDocsRoot> GenerateAsync(string outputPath)
    {
        var assembly = typeof(CommerzbankOptions).Assembly;
        var xmlPath = FindXmlDocPath(assembly);
        _xmlDocs = xmlPath is not null ? LoadXmlDocs(xmlPath) : new Dictionary<string, string>();

        var allTypes = assembly.GetExportedTypes()
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith("Banking.NET", StringComparison.Ordinal))
            .OrderBy(t => t.Namespace, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var enumPairs = allTypes.Where(t => t.IsEnum).Select(t => (Type: t, Doc: BuildEnumDoc(t))).ToList();
        var typePairs = allTypes.Where(t => !t.IsEnum).Select(t => (Type: t, Doc: BuildTypeDoc(t))).ToList();

        AssignSlugs(typePairs, enumPairs);

        var enums = enumPairs
            .Select(p => p.Doc)
            .OrderBy(e => Array.IndexOf(GroupOrder.Values, e.Group))
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToList();

        var types = typePairs
            .Select(p => p.Doc)
            .OrderBy(t => Array.IndexOf(GroupOrder.Values, t.Group))
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var root = new RestApiDocsRoot { Types = types, Enums = enums };

        var json = JsonSerializer.Serialize(root, RestApiDocsJsonContext.Default.RestApiDocsRoot);
        await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
        Console.WriteLine($"  Generated {types.Count} types, {enums.Count} enums -> {Path.GetFileName(outputPath)}");
        return root;
    }

    private static class GroupOrder
    {
        public static readonly string[] Values = ["Configuration", "Authentication", "Exceptions", "Corporate Payments", "ISO 20022"];
    }

    // ── Slugs ──

    /// <summary>
    /// Assigns each type/enum a URL slug. Names are unique today, but two types with the same simple
    /// name (e.g. a generic type or a nested type) would otherwise collide under <c>api/{slug}</c>:
    /// disambiguate first by the last namespace segment, then, if that still collides, by the
    /// reflection type's full name (which, unlike <see cref="Type.Namespace"/>, also includes a
    /// nested type's enclosing type chain). Generic arity suffixes (<c>`1</c>) are stripped from
    /// every slug candidate.
    /// </summary>
    private static void AssignSlugs(List<(Type Type, TypeDoc Doc)> types, List<(Type Type, EnumDoc Doc)> enums)
    {
        var nameCounts = types.Select(p => SlugBase(p.Doc.Name))
            .Concat(enums.Select(p => SlugBase(p.Doc.Name)))
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var p in types)
        {
            var baseName = SlugBase(p.Doc.Name);
            p.Doc.Slug = nameCounts[baseName] > 1 ? $"{baseName}-{LastNamespaceSegment(p.Doc.Namespace)}" : baseName;
        }
        foreach (var p in enums)
        {
            var baseName = SlugBase(p.Doc.Name);
            p.Doc.Slug = nameCounts[baseName] > 1 ? $"{baseName}-{LastNamespaceSegment(p.Doc.Namespace)}" : baseName;
        }

        var slugCounts = types.Select(p => p.Doc.Slug)
            .Concat(enums.Select(p => p.Doc.Slug))
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var p in types)
            if (slugCounts[p.Doc.Slug] > 1) p.Doc.Slug = FullSlug(p.Type);
        foreach (var p in enums)
            if (slugCounts[p.Doc.Slug] > 1) p.Doc.Slug = FullSlug(p.Type);
    }

    private static string SlugBase(string name)
    {
        var tick = name.IndexOf('`');
        return tick < 0 ? name : name[..tick];
    }

    private static string FullSlug(Type type)
    {
        var full = (type.FullName ?? type.Name).Replace('+', '.');
        var tick = full.IndexOf('`');
        if (tick >= 0) full = full[..tick];
        return full.Replace('.', '-');
    }

    private static string LastNamespaceSegment(string ns) => ns.Length == 0 ? "" : ns.Split('.')[^1];

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
            Fields = BuildFieldDocs(type),
            Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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
        // Operators (op_Equality, ...) are IsSpecialName too, but are real, documentable API surface -
        // unlike property/event accessors and other compiler-owned special names, which are excluded.
        if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal)) return false;
        if (method.Name.Contains('<', StringComparison.Ordinal)) return false;
        if (method.Name is "PrintMembers") return false;

        if (kind.StartsWith("Record", StringComparison.Ordinal) && method.Name is "Equals" or "GetHashCode" or "ToString" or "Deconstruct")
            return false;

        return true;
    }

    private ConstructorDoc BuildConstructorDoc(ConstructorInfo ctor)
    {
        var memberId = BuildMemberId(ctor);
        return new ConstructorDoc
        {
            Description = _xmlDocs.GetValueOrDefault(memberId, ""),
            Parameters = ctor.GetParameters().Select(p => BuildParamDoc(p, memberId)).ToList()
        };
    }

    private MethodDoc BuildMethodDoc(MethodInfo method)
    {
        var (memberId, fallbackSource) = ResolveMemberId(method);
        var description = _xmlDocs.GetValueOrDefault(memberId, "");
        if (description.Length == 0 && fallbackSource is not null)
            description = DescribeUndocumentedMember(fallbackSource);

        return new MethodDoc
        {
            Name = FormatMethodName(method),
            Description = description,
            ReturnType = FormatTypeName(method.ReturnType),
            IsStatic = method.IsStatic,
            Parameters = method.GetParameters().Select(p => BuildParamDoc(p, memberId)).ToList()
        };
    }

    private static string FormatMethodName(MethodInfo method)
    {
        if (!method.IsSpecialName) return method.Name;
        return method.Name switch
        {
            "op_Equality" => "operator ==",
            "op_Inequality" => "operator !=",
            "op_LessThan" => "operator <",
            "op_GreaterThan" => "operator >",
            "op_LessThanOrEqual" => "operator <=",
            "op_GreaterThanOrEqual" => "operator >=",
            "op_Addition" => "operator +",
            "op_Subtraction" => "operator -",
            "op_Multiply" => "operator *",
            "op_Division" => "operator /",
            "op_UnaryNegation" => "operator -(unary)",
            "op_UnaryPlus" => "operator +(unary)",
            "op_Implicit" => "implicit operator",
            "op_Explicit" => "explicit operator",
            _ => method.Name
        };
    }

    private List<FieldDoc> BuildFieldDocs(Type type)
    {
        var typeName = GetXmlTypeName(type);
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && !f.IsSpecialName)
            .Select(f => new FieldDoc
            {
                Name = f.Name,
                Type = FormatTypeName(f.FieldType),
                Value = FormatConstValue(f.GetRawConstantValue()),
                Description = _xmlDocs.GetValueOrDefault($"F:{typeName}.{f.Name}", "")
            })
            .ToList();
    }

    private static string FormatConstValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        bool b => b ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    private ParamDocEntry BuildParamDoc(ParameterInfo param, string memberId)
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
            Description = _xmlDocs.GetValueOrDefault($"{memberId}|param:{param.Name}", ""),
            Default = param.HasDefaultValue ? FormatDefaultValue(param.DefaultValue, parameterType) : null
        };
    }

    private PropertyDoc BuildPropertyDoc(PropertyInfo prop)
    {
        var indexParams = prop.GetIndexParameters();
        var name = indexParams.Length == 0
            ? prop.Name
            : $"this[{string.Join(", ", indexParams.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"))}]";

        var (memberId, fallbackSource) = ResolvePropertyMemberId(prop);
        var description = _xmlDocs.GetValueOrDefault(memberId, "");
        if (description.Length == 0 && fallbackSource is not null)
            description = DescribeUndocumentedMember(fallbackSource);

        return new PropertyDoc
        {
            Name = name,
            Type = FormatPropertyTypeName(prop),
            Description = description,
            Accessors = DescribeAccessors(prop),
            Required = IsRequiredMember(prop)
        };
    }

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
        var typeName = GetXmlTypeName(enumType);
        var values = new List<EnumValueDoc>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var field = enumType.GetField(name)!;
            var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
            var rawValue = Convert.ToInt64(Enum.Parse(enumType, name));
            values.Add(new EnumValueDoc
            {
                Name = name,
                SerializedValue = enumMember?.Value ?? rawValue.ToString(CultureInfo.InvariantCulture),
                Description = _xmlDocs.GetValueOrDefault($"F:{typeName}.{name}", "")
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

    // ── XML doc lookup ──

    private string GetXmlSummary(Type type) =>
        _xmlDocs.GetValueOrDefault($"T:{GetXmlTypeName(type)}", "");

    /// <summary>
    /// Resolves the XML doc member ID that actually carries a <c>&lt;summary&gt;</c> for <paramref name="method"/>:
    /// its own ID, or - for an <c>&lt;inheritdoc/&gt;</c> implementation, which the compiler emits without
    /// resolving - the interface member it implements, falling back to the base method it overrides. When
    /// none of those carry a summary in Banking.NET's own XML doc file (e.g. an interface member inherited
    /// from the BCL, such as <see cref="IDisposable.Dispose"/>), the inherited member is also returned so
    /// the caller can fall back to a short description instead of leaving it empty.
    /// </summary>
    private (string Id, MemberInfo? FallbackSource) ResolveMemberId(MethodInfo method)
    {
        var ownId = BuildMemberId(method);
        if (_xmlDocs.ContainsKey(ownId)) return (ownId, null);

        var ifaceMethod = FindInterfaceMethod(method);
        if (ifaceMethod is not null)
        {
            var ifaceId = BuildMemberId(ifaceMethod);
            if (_xmlDocs.ContainsKey(ifaceId)) return (ifaceId, null);
        }

        var baseDefinition = method.GetBaseDefinition();
        if (baseDefinition.DeclaringType != method.DeclaringType)
        {
            var baseId = BuildMemberId(baseDefinition);
            if (_xmlDocs.ContainsKey(baseId)) return (baseId, null);
        }

        MemberInfo? fallbackSource = ifaceMethod is not null
            ? ifaceMethod
            : baseDefinition.DeclaringType != method.DeclaringType ? baseDefinition : null;
        return (ownId, fallbackSource);
    }

    private (string Id, MemberInfo? FallbackSource) ResolvePropertyMemberId(PropertyInfo prop)
    {
        var ownId = BuildMemberId(prop);
        if (_xmlDocs.ContainsKey(ownId)) return (ownId, null);

        var accessor = prop.GetMethod ?? prop.SetMethod;
        var ifaceMethod = accessor is null ? null : FindInterfaceMethod(accessor);
        var ifaceProp = ifaceMethod?.DeclaringType?.GetProperties()
            .FirstOrDefault(p => p.GetMethod == ifaceMethod || p.SetMethod == ifaceMethod);
        if (ifaceProp is not null)
        {
            var ifaceId = BuildMemberId(ifaceProp);
            if (_xmlDocs.ContainsKey(ifaceId)) return (ifaceId, null);
        }

        return (ownId, ifaceProp);
    }

    /// <summary>Short summaries for common BCL members that public types implement via <c>&lt;inheritdoc/&gt;</c> without a local XML doc entry.</summary>
    private static readonly Dictionary<string, string> InheritedMemberSummaries = new(StringComparer.Ordinal)
    {
        ["IDisposable.Dispose"] = "Releases the unmanaged resources and, optionally, the managed resources held by this instance.",
    };

    private static string DescribeUndocumentedMember(MemberInfo source)
    {
        var key = $"{source.DeclaringType!.Name}.{source.Name}";
        return InheritedMemberSummaries.GetValueOrDefault(key, $"Implements {source.DeclaringType.Name}.{source.Name}.");
    }

    /// <summary>Finds the interface method that <paramref name="impl"/> implements, via the declaring type's interface map.</summary>
    private static MethodInfo? FindInterfaceMethod(MethodInfo impl)
    {
        var type = impl.DeclaringType;
        if (type is null || type.IsInterface) return null;

        foreach (var iface in type.GetInterfaces())
        {
            var map = type.GetInterfaceMap(iface);
            var index = Array.IndexOf(map.TargetMethods, impl);
            if (index >= 0) return map.InterfaceMethods[index];
        }

        return null;
    }

    /// <summary>
    /// Builds the exact ECMA-334 XML doc member ID (<c>M:</c>) for a method or constructor, so overloads
    /// resolve to their own <c>&lt;summary&gt;</c> instead of the first one found by name.
    /// </summary>
    private static string BuildMemberId(MethodBase method)
    {
        var typeName = GetXmlTypeName(method.DeclaringType!);
        var memberName = method is ConstructorInfo ? "#ctor" : method.Name;

        if (method is MethodInfo { IsGenericMethodDefinition: true } generic)
            memberName += $"``{generic.GetGenericArguments().Length}";

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"M:{typeName}.{memberName}";

        var paramList = string.Join(",", parameters.Select(p => GetXmlTypeName(p.ParameterType)));
        return $"M:{typeName}.{memberName}({paramList})";
    }

    /// <summary>Builds the exact ECMA-334 XML doc member ID (<c>P:</c>) for a property, including an indexer's parameter list.</summary>
    private static string BuildMemberId(PropertyInfo prop)
    {
        var typeName = GetXmlTypeName(prop.DeclaringType!);
        var indexParams = prop.GetIndexParameters();
        if (indexParams.Length == 0)
            return $"P:{typeName}.{prop.Name}";

        var paramList = string.Join(",", indexParams.Select(p => GetXmlTypeName(p.ParameterType)));
        return $"P:{typeName}.{prop.Name}({paramList})";
    }

    // ── Type formatting ──

    /// <summary>
    /// Formats a type the way the C# compiler encodes it in XML doc member IDs: fully qualified,
    /// nested types joined with '.', closed generic arguments in <c>{...}</c> (not <c>&lt;...&gt;</c>),
    /// arrays as <c>T[]</c>/<c>T[,]</c>, by-ref parameters suffixed with '@', and type parameters as
    /// <c>`N</c> (type-level) or <c>``N</c> (method-level).
    /// </summary>
    private static string GetXmlTypeName(Type type)
    {
        if (type.IsGenericParameter)
            return type.DeclaringMethod is not null ? $"``{type.GenericParameterPosition}" : $"`{type.GenericParameterPosition}";

        if (type.IsByRef)
            return GetXmlTypeName(type.GetElementType()!) + "@";

        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var suffix = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return GetXmlTypeName(type.GetElementType()!) + suffix;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            var definitionName = GetXmlTypeName(type.GetGenericTypeDefinition());
            var tickIndex = definitionName.IndexOf('`');
            var baseName = tickIndex >= 0 ? definitionName[..tickIndex] : definitionName;
            var args = string.Join(",", type.GetGenericArguments().Select(GetXmlTypeName));
            return $"{baseName}{{{args}}}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

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

    private static string? FormatDefaultValue(object? value, Type parameterType)
    {
        // A non-nullable value type (e.g. CancellationToken cancellationToken = default) has no metadata
        // constant to carry "default", so reflection reports DefaultValue as null even though the
        // parameter can never actually be null; show the C# spelling instead of a misleading "null".
        if (value is null && parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
            return "default";

        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            Enum e => e.ToString(),
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString("G", CultureInfo.InvariantCulture),
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

        // A non-nullable value type can never be null; reflection reporting DefaultValue as null for a
        // "= default" struct parameter (see FormatDefaultValue) isn't nullability.
        if (param.ParameterType.IsValueType)
            return false;

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

    // ── XML doc loading ──

    private static string? FindXmlDocPath(Assembly assembly)
    {
        var dllPath = assembly.Location;
        if (string.IsNullOrEmpty(dllPath)) return null;
        var xmlPath = Path.ChangeExtension(dllPath, ".xml");
        return File.Exists(xmlPath) ? xmlPath : null;
    }

    /// <summary>
    /// Loads XML doc summaries, keyed by the exact ECMA-334 member ID. Parameter descriptions are stored
    /// under a synthetic key <c>{memberId}|param:name</c> so they share this dictionary without a second one.
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

    /// <summary>Whether the enum is decorated with <see cref="FlagsAttribute"/>.</summary>
    public bool IsFlags { get; set; }

    /// <summary>The enum members.</summary>
    public List<EnumValueDoc> Values { get; set; } = [];
}

/// <summary>API reference data for a single enum member.</summary>
public class EnumValueDoc
{
    /// <summary>The member name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The wire value used when the member is JSON-serialized (its <see cref="EnumMemberAttribute"/> value, or the numeric value if none is set).</summary>
    public string SerializedValue { get; set; } = "";

    /// <summary>The member's XML doc summary.</summary>
    public string Description { get; set; } = "";
}
