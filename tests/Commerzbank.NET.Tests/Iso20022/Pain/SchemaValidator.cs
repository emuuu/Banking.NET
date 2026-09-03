using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace Commerzbank.NET.Tests.Iso20022.Pain;

/// <summary>Validates XML content against an ISO 20022 XSD schema embedded as a test resource under <c>Iso20022/Schemas/</c>.</summary>
internal static class SchemaValidator
{
    private static readonly Assembly Assembly = typeof(SchemaValidator).Assembly;

    /// <summary>Validates <paramref name="xml"/> against the schema named <paramref name="schemaFileName"/>.</summary>
    /// <returns>The validation error and warning messages, empty when the document is valid.</returns>
    public static IReadOnlyList<string> Validate(string schemaFileName, string xml)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        using (var schemaStream = OpenResource(schemaFileName))
        using (var schemaReader = XmlReader.Create(schemaStream))
            schemas.Add(null, schemaReader);

        var errors = new List<string>();
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);

        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, settings);
        while (reader.Read())
        {
        }

        return errors;
    }

    private static Stream OpenResource(string fileName)
    {
        var resourceName = Assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith(fileName, StringComparison.Ordinal))
            ?? throw new FileNotFoundException($"No embedded resource ending in '{fileName}' was found.");

        return Assembly.GetManifestResourceStream(resourceName)!;
    }
}
