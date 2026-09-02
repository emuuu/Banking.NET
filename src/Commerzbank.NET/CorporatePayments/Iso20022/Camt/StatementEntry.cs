using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A single entry reported in a statement, report or notification (<c>Ntry</c>).</summary>
public sealed class StatementEntry
{
    /// <summary>The entry reference assigned by the account servicer (<c>NtryRef</c>).</summary>
    public string? EntryReference { get; init; }

    /// <summary>The entry amount (<c>Amt</c>).</summary>
    public required Money Amount { get; init; }

    /// <summary>Whether the entry is a credit or a debit (<c>CdtDbtInd</c>).</summary>
    public required CreditDebitIndicator CreditDebit { get; init; }

    /// <summary>Whether this entry reverses a previous entry (<c>RvslInd</c>). Absent in the source XML is read as <see langword="false"/>.</summary>
    public bool IsReversal { get; init; }

    /// <summary>The coded entry status, e.g. <c>BOOK</c> or <c>PDNG</c> (<c>Sts</c> as text in schema version .02; <c>Sts/Cd</c> in .08).</summary>
    public string? Status { get; init; }

    /// <summary>The booking date, without a time component (<c>BookgDt/Dt</c>).</summary>
    public DateOnly? BookingDate { get; init; }

    /// <summary>The booking date and time (<c>BookgDt/DtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? BookingDateTime { get; init; }

    /// <summary>The value date, without a time component (<c>ValDt/Dt</c>).</summary>
    public DateOnly? ValueDate { get; init; }

    /// <summary>The value date and time (<c>ValDt/DtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? ValueDateTime { get; init; }

    /// <summary>The reference assigned by the account servicer (<c>AcctSvcrRef</c>).</summary>
    public string? AccountServicerReference { get; init; }

    /// <summary>The bank transaction code classifying the entry (<c>BkTxCd</c>).</summary>
    public BankTransactionCode? BankTransactionCode { get; init; }

    /// <summary>The batch and per-transaction details underlying this entry (<c>NtryDtls</c>).</summary>
    public List<EntryDetails> Details { get; init; } = [];

    /// <summary>Additional free-form information (<c>AddtlNtryInf</c>).</summary>
    public string? AdditionalEntryInformation { get; init; }

    /// <summary>The <c>Ntry</c> element this entry was read from.</summary>
    public required XElement Source { get; init; }
}
