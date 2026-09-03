using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A bank services billing statement message (camt.086).</summary>
public sealed class BankServicesBillingMessage
{
    /// <summary>The message's identified type, schema identifier and namespace.</summary>
    public required Iso20022MessageIdentifier Identifier { get; init; }

    /// <summary>The report identifier (<c>RptHdr/RptId</c>).</summary>
    public string? ReportId { get; init; }

    /// <summary>The page number, for paginated messages (<c>RptHdr/MsgPgntn/PgNb</c>).</summary>
    public int? PageNumber { get; init; }

    /// <summary>Whether this is the last page (<c>RptHdr/MsgPgntn/LastPgInd</c>).</summary>
    public bool? LastPageIndicator { get; init; }

    /// <summary>The billing statement groups (<c>BllgStmtGrp</c>).</summary>
    public List<BillingStatementGroup> Groups { get; init; } = [];

    /// <summary>The <c>BkSvcsBllgStmt</c> element this message was read from.</summary>
    public required XElement Source { get; init; }
}
