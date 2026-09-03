using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The status of a single original transaction (ISO 20022 <c>PaymentTransactionInformation25</c>/<c>PaymentTransaction105</c>, <c>TxInfAndSts</c>).</summary>
public sealed class TransactionStatus
{
    /// <summary>The status identifier (<c>StsId</c>).</summary>
    public string? StatusId { get; init; }

    /// <summary>The original instruction identifier (<c>OrgnlInstrId</c>).</summary>
    public string? OriginalInstructionId { get; init; }

    /// <summary>The original end-to-end identifier (<c>OrgnlEndToEndId</c>).</summary>
    public string? OriginalEndToEndId { get; init; }

    /// <summary>The original unique end-to-end transaction reference (<c>OrgnlUETR</c>, schema version .10 only).</summary>
    public string? OriginalUetr { get; init; }

    /// <summary>The transaction status, e.g. <c>ACSC</c>, <c>RJCT</c> (<c>TxSts</c>).</summary>
    public string? Status { get; init; }

    /// <summary>The reasons for the transaction status (<c>StsRsnInf</c>).</summary>
    public List<StatusReason> StatusReasons { get; init; } = [];

    /// <summary>The date and time the original transaction was accepted (<c>AccptncDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? AcceptanceDateTime { get; init; }

    /// <summary>The reference assigned by the account servicer (<c>AcctSvcrRef</c>).</summary>
    public string? AccountServicerReference { get; init; }

    /// <summary>The clearing system reference (<c>ClrSysRef</c>).</summary>
    public string? ClearingSystemReference { get; init; }

    /// <summary>Key information about the original transaction (<c>OrgnlTxRef</c>).</summary>
    public OriginalTransactionReference? OriginalTransaction { get; init; }

    /// <summary>The <c>TxInfAndSts</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
