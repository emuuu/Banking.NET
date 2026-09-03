using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A single report, statement or notification (<c>Rpt</c>/<c>Stmt</c>/<c>Ntfctn</c>) within a <see cref="BankToCustomerMessage"/>.</summary>
public sealed class AccountStatement
{
    /// <summary>Which camt message kind this statement was read from.</summary>
    public required StatementKind Kind { get; init; }

    /// <summary>The statement identifier (<c>Id</c>).</summary>
    public string? Id { get; init; }

    /// <summary>The electronic sequence number (<c>ElctrncSeqNb</c>).</summary>
    public long? ElectronicSequenceNumber { get; init; }

    /// <summary>The legal sequence number (<c>LglSeqNb</c>).</summary>
    public long? LegalSequenceNumber { get; init; }

    /// <summary>The statement creation date and time (<c>CreDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? CreationDateTime { get; init; }

    /// <summary>The start of the reporting period (<c>FrToDt/FrDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? FromDateTime { get; init; }

    /// <summary>The end of the reporting period (<c>FrToDt/ToDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? ToDateTime { get; init; }

    /// <summary>The coded copy/duplicate indicator (<c>CpyDplctInd</c>).</summary>
    public string? CopyDuplicateIndicator { get; init; }

    /// <summary>The reported account (<c>Acct</c>).</summary>
    public AccountIdentification? Account { get; init; }

    /// <summary>The account owner's name (<c>Acct/Ownr/Nm</c>).</summary>
    public string? AccountOwnerName { get; init; }

    /// <summary>The account servicing institution (<c>Acct/Svcr</c>).</summary>
    public FinancialInstitution? AccountServicer { get; init; }

    /// <summary>The reported balances (<c>Bal</c>).</summary>
    public List<Balance> Balances { get; init; } = [];

    /// <summary>The entries and totals summary (<c>TxsSummry</c>).</summary>
    public TransactionsSummary? TransactionsSummary { get; init; }

    /// <summary>The reported entries (<c>Ntry</c>).</summary>
    public List<StatementEntry> Entries { get; init; } = [];

    /// <summary>Additional free-form information (<c>AddtlStmtInf</c>).</summary>
    public string? AdditionalStatementInformation { get; init; }

    /// <summary>The <c>Rpt</c>/<c>Stmt</c>/<c>Ntfctn</c> element this statement was read from.</summary>
    public required XElement Source { get; init; }
}
