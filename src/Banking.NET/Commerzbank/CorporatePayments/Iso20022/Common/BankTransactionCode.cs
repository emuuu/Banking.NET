using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>The bank transaction code classifying a transaction (ISO 20022 <c>BankTransactionCodeStructure</c>).</summary>
public sealed class BankTransactionCode
{
    /// <summary>The domain code (<c>Domn/Cd</c>).</summary>
    public string? Domain { get; set; }

    /// <summary>The family code (<c>Domn/Fmly/Cd</c>).</summary>
    public string? Family { get; set; }

    /// <summary>The sub-family code (<c>Domn/Fmly/SubFmlyCd</c>).</summary>
    public string? SubFamily { get; set; }

    /// <summary>The proprietary code (<c>Prtry/Cd</c>), used instead of the domain/family/sub-family codes.</summary>
    public string? ProprietaryCode { get; set; }

    /// <summary>The issuer of the proprietary code (<c>Prtry/Issr</c>).</summary>
    public string? ProprietaryIssuer { get; set; }

    /// <summary>The XML element this bank transaction code was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
