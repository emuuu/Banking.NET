using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A single billing statement for one account (<c>BllgStmt</c>).</summary>
public sealed class BillingStatement
{
    /// <summary>The statement identifier (<c>StmtId</c>).</summary>
    public string? StatementId { get; init; }

    /// <summary>The start of the billing period (<c>FrToDt/FrDt</c>).</summary>
    public DateOnly? FromDate { get; init; }

    /// <summary>The end of the billing period (<c>FrToDt/ToDt</c>).</summary>
    public DateOnly? ToDate { get; init; }

    /// <summary>The statement creation date and time (<c>CreDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? CreationDateTime { get; init; }

    /// <summary>The coded statement status (<c>Sts</c>): <c>ORGN</c>/<c>RPLC</c>/<c>TEST</c>.</summary>
    public string? Status { get; init; }

    /// <summary>The coded account level, e.g. whether this is a consolidated statement (<c>AcctChrtcs/AcctLvl</c>): <c>INTM</c>/<c>SMRY</c>/<c>DETL</c>.</summary>
    public string? AccountLevel { get; init; }

    /// <summary>The billed account (<c>AcctChrtcs/CshAcct</c>).</summary>
    public AccountIdentification? Account { get; init; }

    /// <summary>The account servicing institution (<c>AcctChrtcs/AcctSvcr</c>).</summary>
    public FinancialInstitution? AccountServicer { get; init; }

    /// <summary>The coded compensation method for account charges (<c>AcctChrtcs/CompstnMtd</c>): <c>NOCP</c>/<c>DBTD</c>/<c>INVD</c>/<c>DDBT</c>.</summary>
    public string? CompensationMethod { get; init; }

    /// <summary>The account balance currency (<c>AcctChrtcs/AcctBalCcyCd</c>).</summary>
    public string? AccountBalanceCurrency { get; init; }

    /// <summary>The settlement currency (<c>AcctChrtcs/SttlmCcyCd</c>).</summary>
    public string? SettlementCurrency { get; init; }

    /// <summary>The host system currency (<c>AcctChrtcs/HstCcyCd</c>).</summary>
    public string? HostCurrency { get; init; }

    /// <summary>The reported balances (<c>Bal</c>).</summary>
    public List<BillingBalance> Balances { get; init; } = [];

    /// <summary>The billed services (<c>Svc</c>).</summary>
    public List<BillingService> Services { get; init; } = [];

    /// <summary>The tax regions applicable to the statement (<c>TaxRgn</c>).</summary>
    public List<BillingTaxRegion> TaxRegions { get; init; } = [];

    /// <summary>The <c>BllgStmt</c> element this statement was read from.</summary>
    public required XElement Source { get; init; }
}
