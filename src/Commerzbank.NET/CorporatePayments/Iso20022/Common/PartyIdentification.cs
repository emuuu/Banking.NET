using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Identifies a party, e.g. a debtor or creditor (ISO 20022 <c>PartyIdentification</c>).</summary>
public sealed class PartyIdentification
{
    /// <summary>The party's name (<c>Nm</c>).</summary>
    public string? Name { get; set; }

    /// <summary>The party's postal address (<c>PstlAdr</c>).</summary>
    public PostalAddress? PostalAddress { get; set; }

    /// <summary>The party's organisation identification (<c>Id/OrgId</c>), when the party is an organisation.</summary>
    public OrganisationIdentification? OrganisationId { get; set; }

    /// <summary>The party's private identification (<c>Id/PrvtId</c>), when the party is a private individual.</summary>
    public PrivateIdentification? PrivateId { get; set; }

    /// <summary>The party's country of residence (<c>CtryOfRes</c>).</summary>
    public string? CountryOfResidence { get; set; }

    /// <summary>The XML element this party was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
