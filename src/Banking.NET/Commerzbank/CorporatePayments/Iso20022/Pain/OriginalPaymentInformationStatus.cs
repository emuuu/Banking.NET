using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>The status of a single original payment information block a status report refers to (ISO 20022 <c>OriginalPaymentInstruction...AndStatus</c>, <c>OrgnlPmtInfAndSts</c>).</summary>
public sealed class OriginalPaymentInformationStatus
{
    /// <summary>The original payment information identifier (<c>OrgnlPmtInfId</c>).</summary>
    public string? OriginalPaymentInformationId { get; init; }

    /// <summary>The number of transactions in the original payment information block (<c>OrgnlNbOfTxs</c>).</summary>
    public int? OriginalNumberOfTransactions { get; init; }

    /// <summary>The total of the amounts of all transactions in the original payment information block (<c>OrgnlCtrlSum</c>).</summary>
    public decimal? OriginalControlSum { get; init; }

    /// <summary>The status of the payment information block, e.g. <c>ACCP</c>, <c>RJCT</c> (<c>PmtInfSts</c>).</summary>
    public string? PaymentInformationStatus { get; init; }

    /// <summary>The reasons for the payment information status (<c>StsRsnInf</c>).</summary>
    public List<StatusReason> StatusReasons { get; init; } = [];

    /// <summary>The number of transactions per detailed status (<c>NbOfTxsPerSts</c>).</summary>
    public List<TransactionCountPerStatus> NumberOfTransactionsPerStatus { get; init; } = [];

    /// <summary>The status of the individual transactions in the original payment information block (<c>TxInfAndSts</c>).</summary>
    public List<TransactionStatus> Transactions { get; init; } = [];

    /// <summary>The <c>OrgnlPmtInfAndSts</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
