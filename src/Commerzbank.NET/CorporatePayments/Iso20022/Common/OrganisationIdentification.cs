namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Identifies an organisation (ISO 20022 <c>OrganisationIdentification</c>).</summary>
public sealed class OrganisationIdentification
{
    /// <summary>The BIC or BEI (<c>AnyBIC</c> in newer schema versions, <c>BICOrBEI</c> in older ones).</summary>
    public string? Bic { get; set; }

    /// <summary>The identifier value of a non-BIC scheme (<c>Othr/Id</c>).</summary>
    public string? OtherId { get; set; }

    /// <summary>The coded scheme name of a non-BIC identifier (<c>Othr/SchmeNm/Cd</c>).</summary>
    public string? OtherSchemeCode { get; set; }

    /// <summary>The proprietary scheme name of a non-BIC identifier (<c>Othr/SchmeNm/Prtry</c>).</summary>
    public string? OtherSchemeProprietary { get; set; }

    /// <summary>The issuer of a non-BIC identifier (<c>Othr/Issr</c>).</summary>
    public string? OtherIssuer { get; set; }
}
