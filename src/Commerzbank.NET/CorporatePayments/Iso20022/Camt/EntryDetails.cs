using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Batch and per-transaction details underlying a <see cref="StatementEntry"/> (<c>NtryDtls</c>).</summary>
public sealed class EntryDetails
{
    /// <summary>The batch message identifier (<c>Btch/MsgId</c>).</summary>
    public string? BatchMessageId { get; init; }

    /// <summary>The batch payment information identifier (<c>Btch/PmtInfId</c>).</summary>
    public string? BatchPaymentInformationId { get; init; }

    /// <summary>The number of transactions in the batch (<c>Btch/NbOfTxs</c>).</summary>
    public int? BatchNumberOfTransactions { get; init; }

    /// <summary>The total amount of the batch (<c>Btch/TtlAmt</c>).</summary>
    public Money? BatchTotalAmount { get; init; }

    /// <summary>Whether the batch total is a credit or a debit (<c>Btch/CdtDbtInd</c>).</summary>
    public CreditDebitIndicator? BatchCreditDebit { get; init; }

    /// <summary>The individual transactions in this batch (<c>TxDtls</c>).</summary>
    public List<TransactionDetails> Transactions { get; init; } = [];

    /// <summary>The <c>NtryDtls</c> element these details were read from.</summary>
    public required XElement Source { get; init; }
}
