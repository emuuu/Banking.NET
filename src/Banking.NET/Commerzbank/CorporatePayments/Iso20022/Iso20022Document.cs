using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Loads and identifies ISO 20022 XML documents.</summary>
public static class Iso20022Document
{
    private const string NamespacePrefix = "urn:iso:std:iso:20022:tech:xsd:";

    private static readonly Regex SchemaIdentifierPattern =
        new(@"^(camt|pain)\.\d{3}\.\d{3}\.\d{2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Loads an XML document from a stream. DTD processing is prohibited and external entity resolution is disabled to prevent XXE attacks.</summary>
    /// <param name="xml">The stream to read the document from. The stream is not closed by this method.</param>
    /// <returns>The loaded document.</returns>
    public static XDocument Load(Stream xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var reader = XmlReader.Create(xml, ReaderSettings);
        return XDocument.Load(reader);
    }

    /// <summary>Loads an XML document from a string. DTD processing is prohibited and external entity resolution is disabled to prevent XXE attacks.</summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>The loaded document.</returns>
    public static XDocument Load(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, ReaderSettings);
        return XDocument.Load(reader);
    }

    /// <summary>Identifies the ISO 20022 message type of a document from its root element's XML namespace.</summary>
    /// <param name="document">The document to identify.</param>
    /// <returns>
    /// The identified message type, schema identifier and namespace. When the root element's namespace does not start
    /// with the ISO 20022 tech XSD namespace prefix, <see cref="Iso20022MessageIdentifier.Type"/> is
    /// <see cref="Iso20022MessageType.Unknown"/> and <see cref="Iso20022MessageIdentifier.Identifier"/> is empty. When
    /// the prefix is present but the remainder does not match a well-formed ISO 20022 schema identifier
    /// (<c>{camt|pain}.NNN.NNN.NN</c>) or does not match a known message type, <see cref="Iso20022MessageIdentifier.Type"/>
    /// is <see cref="Iso20022MessageType.Unknown"/> while <see cref="Iso20022MessageIdentifier.Identifier"/> still
    /// carries the full remainder.
    /// </returns>
    /// <exception cref="InvalidOperationException">The document has no root element.</exception>
    public static Iso20022MessageIdentifier Identify(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root ?? throw new InvalidOperationException("The document has no root element.");
        var ns = root.Name.NamespaceName;

        if (!ns.StartsWith(NamespacePrefix, StringComparison.Ordinal))
            return new Iso20022MessageIdentifier(Iso20022MessageType.Unknown, string.Empty, ns);

        var identifier = ns[NamespacePrefix.Length..];
        var type = SchemaIdentifierPattern.IsMatch(identifier) ? IdentifyType(identifier) : Iso20022MessageType.Unknown;
        return new Iso20022MessageIdentifier(type, identifier, ns);
    }

    private static Iso20022MessageType IdentifyType(string identifier) =>
        identifier[..8] switch
        {
            "camt.052" => Iso20022MessageType.Camt052,
            "camt.053" => Iso20022MessageType.Camt053,
            "camt.054" => Iso20022MessageType.Camt054,
            "camt.086" => Iso20022MessageType.Camt086,
            "pain.001" => Iso20022MessageType.Pain001,
            "pain.002" => Iso20022MessageType.Pain002,
            "pain.008" => Iso20022MessageType.Pain008,
            _ => Iso20022MessageType.Unknown,
        };

    private static XmlReaderSettings ReaderSettings => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };
}
