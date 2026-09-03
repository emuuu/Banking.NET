using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Summary totals for the entries reported in a statement (<c>TxsSummry</c>).</summary>
public sealed class TransactionsSummary
{
    /// <summary>The total number of entries (<c>TtlNtries/NbOfNtries</c>).</summary>
    public int? TotalEntries { get; init; }

    /// <summary>The total sum across all entries, regardless of sign (<c>TtlNtries/Sum</c>).</summary>
    public decimal? TotalSum { get; init; }

    /// <summary>The net amount across all entries (<c>TtlNtries/TtlNetNtry/Amt</c>).</summary>
    public Money? TotalNetEntry { get; init; }

    /// <summary>Whether the net amount is a net credit or net debit (<c>TtlNtries/TtlNetNtry/CdtDbtInd</c> or <c>TtlNtries/CdtDbtInd</c>).</summary>
    public CreditDebitIndicator? TotalNetCreditDebit { get; init; }

    /// <summary>The number of credit entries (<c>TtlCdtNtries/NbOfNtries</c>).</summary>
    public int? TotalCreditEntries { get; init; }

    /// <summary>The sum of credit entries (<c>TtlCdtNtries/Sum</c>).</summary>
    public decimal? TotalCreditSum { get; init; }

    /// <summary>The number of debit entries (<c>TtlDbtNtries/NbOfNtries</c>).</summary>
    public int? TotalDebitEntries { get; init; }

    /// <summary>The sum of debit entries (<c>TtlDbtNtries/Sum</c>).</summary>
    public decimal? TotalDebitSum { get; init; }

    /// <summary>The <c>TxsSummry</c> element this summary was read from.</summary>
    public required XElement Source { get; init; }
}
