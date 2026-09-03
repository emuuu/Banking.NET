namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Identifies a private individual (ISO 20022 <c>PersonIdentification</c>).</summary>
public sealed class PrivateIdentification
{
    /// <summary>The date of birth (<c>DtAndPlcOfBirth/BirthDt</c>).</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>The city of birth (<c>DtAndPlcOfBirth/CityOfBirth</c>).</summary>
    public string? CityOfBirth { get; set; }

    /// <summary>The country of birth (<c>DtAndPlcOfBirth/CtryOfBirth</c>).</summary>
    public string? CountryOfBirth { get; set; }

    /// <summary>The identifier value of a non-birth-data scheme (<c>Othr/Id</c>).</summary>
    public string? OtherId { get; set; }

    /// <summary>The coded scheme name of a non-birth-data identifier (<c>Othr/SchmeNm/Cd</c>).</summary>
    public string? OtherSchemeCode { get; set; }

    /// <summary>The proprietary scheme name of a non-birth-data identifier (<c>Othr/SchmeNm/Prtry</c>).</summary>
    public string? OtherSchemeProprietary { get; set; }

    /// <summary>The issuer of a non-birth-data identifier (<c>Othr/Issr</c>).</summary>
    public string? OtherIssuer { get; set; }
}
