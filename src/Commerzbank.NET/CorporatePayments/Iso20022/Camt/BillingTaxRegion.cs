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

    /// <summary>The settlement amount for this region (<c>SttlmAmt/Amt</c>). Negative when <c>SttlmAmt/Sgn</c> is <see langword="true"/>.</summary>
    public Money? SettlementAmount { get; init; }

    /// <summary>The tax amount actually due to this region (<c>TaxDueToRgn/Amt</c>). Negative when <c>TaxDueToRgn/Sgn</c> is <see langword="true"/>.</summary>
    public Money? TaxDueToRegion { get; init; }

    /// <summary>The <c>TaxRgn</c> element this tax region was read from.</summary>
    public required XElement Source { get; init; }
}
