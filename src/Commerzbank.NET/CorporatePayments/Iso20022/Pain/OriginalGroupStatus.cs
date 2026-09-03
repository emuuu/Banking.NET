using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The status of the original group of payments a status report refers to (ISO 20022 <c>OriginalGroupInformationAndStatus</c>).</summary>
public sealed class OriginalGroupStatus
{
    /// <summary>The original message identifier (<c>OrgnlMsgId</c>).</summary>
    public string? OriginalMessageId { get; init; }

    /// <summary>The original message name identifier, e.g. <c>pain.001.001.09</c> (<c>OrgnlMsgNmId</c>).</summary>
    public string? OriginalMessageNameId { get; init; }

    /// <summary>The creation date and time of the original message (<c>OrgnlCreDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? OriginalCreationDateTime { get; init; }

    /// <summary>The number of transactions in the original message (<c>OrgnlNbOfTxs</c>).</summary>
    public int? OriginalNumberOfTransactions { get; init; }

    /// <summary>The total of the amounts of all transactions in the original message (<c>OrgnlCtrlSum</c>).</summary>
    public decimal? OriginalControlSum { get; init; }

    /// <summary>The status of the original group, e.g. <c>ACCP</c>, <c>RJCT</c> (<c>GrpSts</c>).</summary>
    public string? GroupStatus { get; init; }

    /// <summary>The reasons for the group status (<c>StsRsnInf</c>).</summary>
    public List<StatusReason> StatusReasons { get; init; } = [];

    /// <summary>The number of transactions per detailed status (<c>NbOfTxsPerSts</c>).</summary>
    public List<TransactionCountPerStatus> NumberOfTransactionsPerStatus { get; init; } = [];

    /// <summary>The <c>OrgnlGrpInfAndSts</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
