using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Remittance information attached to a payment (ISO 20022 <c>RemittanceInformation</c>).</summary>
public sealed class RemittanceInformation
{
    /// <summary>Unstructured remittance information lines (<c>Ustrd</c>). SEPA payments carry exactly one entry.</summary>
    public List<string> Unstructured { get; set; } = [];

    /// <summary>The structured creditor reference (<c>Strd/CdtrRefInf/Ref</c>).</summary>
    public string? CreditorReference { get; set; }

    /// <summary>The coded creditor reference type (<c>Strd/CdtrRefInf/Tp/CdOrPrtry/Cd</c>).</summary>
    public string? CreditorReferenceTypeCode { get; set; }

    /// <summary>The proprietary creditor reference type (<c>Strd/CdtrRefInf/Tp/CdOrPrtry/Prtry</c>).</summary>
    public string? CreditorReferenceTypeProprietary { get; set; }

    /// <summary>The issuer of the creditor reference type (<c>Strd/CdtrRefInf/Tp/Issr</c>).</summary>
    public string? CreditorReferenceIssuer { get; set; }

    /// <summary>The XML element this remittance information was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
