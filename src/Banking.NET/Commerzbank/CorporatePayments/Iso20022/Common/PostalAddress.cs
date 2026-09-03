using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A postal address (ISO 20022 <c>PostalAddress</c> variants).</summary>
public sealed class PostalAddress
{
    /// <summary>The ISO 3166-1 alpha-2 country code (<c>Ctry</c>).</summary>
    public string? Country { get; set; }

    /// <summary>Free-form address lines (<c>AdrLine</c>).</summary>
    public List<string> AddressLines { get; set; } = [];

    /// <summary>The street name (<c>StrtNm</c>).</summary>
    public string? StreetName { get; set; }

    /// <summary>The building number (<c>BldgNb</c>).</summary>
    public string? BuildingNumber { get; set; }

    /// <summary>The post code (<c>PstCd</c>).</summary>
    public string? PostCode { get; set; }

    /// <summary>The town name (<c>TwnNm</c>).</summary>
    public string? TownName { get; set; }

    /// <summary>The department (<c>Dept</c>).</summary>
    public string? Department { get; set; }

    /// <summary>The country subdivision, e.g. a state or region (<c>CtrySubDvsn</c>).</summary>
    public string? CountrySubDivision { get; set; }

    /// <summary>The XML element this address was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
