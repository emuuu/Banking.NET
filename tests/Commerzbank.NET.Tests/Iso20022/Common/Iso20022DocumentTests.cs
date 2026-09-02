using System.Text;
using System.Xml;
using System.Xml.Linq;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Common;

public class Iso20022DocumentTests
{
    public static TheoryData<string, Iso20022MessageType, string> KnownNamespaces => new()
    {
        { "urn:iso:std:iso:20022:tech:xsd:camt.052.001.08", Iso20022MessageType.Camt052, "camt.052.001.08" },
        { "urn:iso:std:iso:20022:tech:xsd:camt.053.001.02", Iso20022MessageType.Camt053, "camt.053.001.02" },
        { "urn:iso:std:iso:20022:tech:xsd:camt.053.001.08", Iso20022MessageType.Camt053, "camt.053.001.08" },
        { "urn:iso:std:iso:20022:tech:xsd:camt.054.001.08", Iso20022MessageType.Camt054, "camt.054.001.08" },
        { "urn:iso:std:iso:20022:tech:xsd:camt.086.001.02", Iso20022MessageType.Camt086, "camt.086.001.02" },
        { "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09", Iso20022MessageType.Pain001, "pain.001.001.09" },
        { "urn:iso:std:iso:20022:tech:xsd:pain.002.001.10", Iso20022MessageType.Pain002, "pain.002.001.10" },
        { "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08", Iso20022MessageType.Pain008, "pain.008.001.08" },
    };

    [Theory]
    [MemberData(nameof(KnownNamespaces))]
    public void Identify_KnownNamespace_ReturnsTypeAndIdentifier(string ns, Iso20022MessageType expectedType, string expectedIdentifier)
    {
        var document = new XDocument(new XElement(XName.Get("Document", ns)));
        var identifier = Iso20022Document.Identify(document);

        identifier.Type.ShouldBe(expectedType);
        identifier.Identifier.ShouldBe(expectedIdentifier);
        identifier.Namespace.ShouldBe(ns);
    }

    [Fact]
    public void Identify_UnrelatedNamespace_ReturnsUnknownWithEmptyIdentifier()
    {
        var document = new XDocument(new XElement(XName.Get("Document", "urn:example:other")));
        var identifier = Iso20022Document.Identify(document);

        identifier.Type.ShouldBe(Iso20022MessageType.Unknown);
        identifier.Identifier.ShouldBe(string.Empty);
    }

    [Fact]
    public void Identify_NoNamespace_ReturnsUnknownWithEmptyIdentifier()
    {
        var document = new XDocument(new XElement("Document"));
        var identifier = Iso20022Document.Identify(document);

        identifier.Type.ShouldBe(Iso20022MessageType.Unknown);
        identifier.Identifier.ShouldBe(string.Empty);
    }

    [Fact]
    public void Identify_NoRootElement_ThrowsInvalidOperationException()
    {
        var document = new XDocument();
        Should.Throw<InvalidOperationException>(() => Iso20022Document.Identify(document));
    }

    public static TheoryData<string> MalformedSchemaIdentifierNamespaces => new()
    {
        "urn:iso:std:iso:20022:tech:xsd:camt.053x",
        "urn:iso:std:iso:20022:tech:xsd:camt.053",
        "urn:iso:std:iso:20022:tech:xsd:camt.053-malformed",
    };

    [Theory]
    [MemberData(nameof(MalformedSchemaIdentifierNamespaces))]
    public void Identify_MalformedSchemaIdentifier_ReturnsUnknown(string ns)
    {
        var document = new XDocument(new XElement(XName.Get("Document", ns)));
        var identifier = Iso20022Document.Identify(document);

        identifier.Type.ShouldBe(Iso20022MessageType.Unknown);
    }

    [Fact]
    public void Identify_KnownPrefixUnknownMessageType_ReturnsUnknownWithNonEmptyIdentifier()
    {
        const string ns = "urn:iso:std:iso:20022:tech:xsd:camt.999.001.01";
        var document = new XDocument(new XElement(XName.Get("Document", ns)));
        var identifier = Iso20022Document.Identify(document);

        identifier.Type.ShouldBe(Iso20022MessageType.Unknown);
        identifier.Identifier.ShouldBe("camt.999.001.01");
    }

    [Fact]
    public void Load_String_ParsesDocument()
    {
        var document = Iso20022Document.Load("<a><b>1</b></a>");
        document.Root!.Name.LocalName.ShouldBe("a");
    }

    [Fact]
    public void Load_Stream_ParsesDocument()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<a><b>1</b></a>"));
        var document = Iso20022Document.Load(stream);
        document.Root!.Name.LocalName.ShouldBe("a");
    }

    [Fact]
    public void Load_StringWithDtd_ThrowsXmlException()
    {
        const string xml = "<?xml version=\"1.0\"?><!DOCTYPE a [<!ENTITY x \"y\">]><a>&x;</a>";
        Should.Throw<XmlException>(() => Iso20022Document.Load(xml));
    }

    [Fact]
    public void Load_StreamWithDtd_ThrowsXmlException()
    {
        const string xml = "<?xml version=\"1.0\"?><!DOCTYPE a [<!ENTITY x \"y\">]><a>&x;</a>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        Should.Throw<XmlException>(() => Iso20022Document.Load(stream));
    }
}
