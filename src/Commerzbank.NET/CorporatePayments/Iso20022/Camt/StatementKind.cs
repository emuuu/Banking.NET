namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The kind of camt message an <see cref="AccountStatement"/> was read from.</summary>
public enum StatementKind
{
    /// <summary>An intraday account report (camt.052, root child <c>Rpt</c>).</summary>
    Report,

    /// <summary>An account statement (camt.053, root child <c>Stmt</c>).</summary>
    Statement,

    /// <summary>A debit/credit notification (camt.054, root child <c>Ntfctn</c>).</summary>
    Notification,
}
