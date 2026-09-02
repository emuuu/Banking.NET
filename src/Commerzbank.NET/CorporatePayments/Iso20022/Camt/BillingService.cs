using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A single billed banking service (<c>Svc</c>).</summary>
public sealed class BillingService
{
    /// <summary>The service identifier (<c>SvcDtl/BkSvc/Id</c>).</summary>
    public string? ServiceId { get; init; }

    /// <summary>The sub-service code (<c>SvcDtl/BkSvc/SubSvc/Id</c>).</summary>
    public string? SubServiceCode { get; init; }

    /// <summary>The coded issuer of the sub-service (<c>SvcDtl/BkSvc/SubSvc/Issr/Cd</c>).</summary>
    public string? SubServiceIssuer { get; init; }

    /// <summary>The service description (<c>SvcDtl/BkSvc/Desc</c>).</summary>
    public string? Description { get; init; }

    /// <summary>The common (bank-independent) service code (<c>SvcDtl/BkSvc/CmonCd/Cd</c>).</summary>
    public string? CommonCode { get; init; }

    /// <summary>The coded service type (<c>SvcDtl/BkSvc/SvcTp</c>).</summary>
    public string? ServiceType { get; init; }

    /// <summary>The bank transaction code of the service (<c>SvcDtl/BkSvc/BkTxCd</c>).</summary>
    public BankTransactionCode? BankTransactionCode { get; init; }

    /// <summary>The billed volume (<c>SvcDtl/Vol</c>).</summary>
    public decimal? Volume { get; init; }

    /// <summary>The pricing currency (<c>Pric/Ccy</c>).</summary>
    public string? PriceCurrency { get; init; }

    /// <summary>The unit price (<c>Pric/UnitPric/Amt</c>).</summary>
    public Money? UnitPrice { get; init; }

    /// <summary>The coded pricing method (<c>Pric/Mtd</c>).</summary>
    public string? PriceMethod { get; init; }

    /// <summary>The coded pricing rule (<c>Pric/Rule</c>).</summary>
    public string? PriceRule { get; init; }

    /// <summary>The coded payment method for the charge (<c>PmtMtd</c>).</summary>
    public string? PaymentMethod { get; init; }

    /// <summary>The original charge price before any adjustments (<c>OrgnlChrgPric/Amt</c>).</summary>
    public Money? OriginalChargePrice { get; init; }

    /// <summary>The original charge amount in the settlement currency (<c>OrgnlChrgSttlmAmt/Amt</c>).</summary>
    public Money? OriginalChargeSettlementAmount { get; init; }

    /// <summary>The account balance required to offset this charge (<c>BalReqrdAcctAmt/Amt</c>).</summary>
    public Money? BalanceRequiredAmount { get; init; }

    /// <summary>The coded tax designation (<c>TaxDsgnt/Cd</c>).</summary>
    public string? TaxDesignation { get; init; }

    /// <summary>The <c>Svc</c> element this service was read from.</summary>
    public required XElement Source { get; init; }
}
