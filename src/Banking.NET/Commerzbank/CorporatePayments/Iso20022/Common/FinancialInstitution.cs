using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Identifies a financial institution, e.g. a debtor or creditor agent (ISO 20022 <c>FinancialInstitutionIdentification</c>).</summary>
public sealed class FinancialInstitution
{
    /// <summary>The BIC (<c>BIC</c> in older schema versions, <c>BICFI</c> in newer ones).</summary>
    public string? Bic { get; set; }

    /// <summary>The institution's name (<c>Nm</c>).</summary>
    public string? Name { get; set; }

    /// <summary>The clearing system member identifier (<c>ClrSysMmbId/MmbId</c>).</summary>
    public string? ClearingSystemMemberId { get; set; }

    /// <summary>The identifier value of a non-BIC, non-clearing-system identification (<c>Othr/Id</c>).</summary>
    public string? OtherId { get; set; }

    /// <summary>The institution's postal address (<c>PstlAdr</c>).</summary>
    public PostalAddress? PostalAddress { get; set; }

    /// <summary>The XML element this financial institution was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
