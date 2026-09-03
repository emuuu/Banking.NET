using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Identifies a cash account (ISO 20022 <c>CashAccount</c>).</summary>
public sealed class AccountIdentification
{
    /// <summary>The IBAN (<c>Id/IBAN</c>).</summary>
    public string? Iban { get; set; }

    /// <summary>The identifier value of a non-IBAN account (<c>Id/Othr/Id</c>).</summary>
    public string? OtherId { get; set; }

    /// <summary>The coded scheme name of a non-IBAN account identifier (<c>Id/Othr/SchmeNm/Cd</c>).</summary>
    public string? OtherSchemeCode { get; set; }

    /// <summary>The account currency (<c>Ccy</c>).</summary>
    public string? Currency { get; set; }

    /// <summary>The account name (<c>Nm</c>).</summary>
    public string? Name { get; set; }

    /// <summary>The coded account type (<c>Tp/Cd</c>).</summary>
    public string? TypeCode { get; set; }

    /// <summary>The XML element this account was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
