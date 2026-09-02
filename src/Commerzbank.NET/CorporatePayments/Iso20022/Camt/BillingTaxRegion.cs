using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A tax region applicable to a billing statement (<c>TaxRgn</c>).</summary>
public sealed class BillingTaxRegion
{
    /// <summary>The region number (<c>RgnNb</c>).</summary>
    public string? RegionNumber { get; init; }

    /// <summary>The region name (<c>RgnNm</c>).</summary>
    public string? RegionName { get; init; }

    /// <summary>The customer's tax identifier in this region (<c>CstmrTaxId</c>).</summary>
    public string? CustomerTaxId { get; init; }

    /// <summary>The total tax amount for this region (<c>TtlTaxAmt/Amt</c>).</summary>
    public Money? TotalTaxAmount { get; init; }

    /// <summary>The <c>TaxRgn</c> element this tax region was read from.</summary>
    public required XElement Source { get; init; }
}
