using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A pain.002 customer payment status report message.</summary>
public sealed class PaymentStatusReport
{
    /// <summary>The message's identified type, schema identifier and namespace.</summary>
    public required Iso20022MessageIdentifier Identifier { get; init; }

    /// <summary>The message identifier (<c>GrpHdr/MsgId</c>).</summary>
    public string? MessageId { get; init; }

    /// <summary>The message creation date and time (<c>GrpHdr/CreDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? CreationDateTime { get; init; }

    /// <summary>The party that initiated the status report (<c>GrpHdr/InitgPty</c>).</summary>
    public PartyIdentification? InitiatingParty { get; init; }

    /// <summary>The debtor's agent (<c>GrpHdr/DbtrAgt</c>).</summary>
    public FinancialInstitution? DebtorAgent { get; init; }

    /// <summary>The creditor's agent (<c>GrpHdr/CdtrAgt</c>).</summary>
    public FinancialInstitution? CreditorAgent { get; init; }

    /// <summary>The status of the original message this report refers to (<c>OrgnlGrpInfAndSts</c>).</summary>
    public required OriginalGroupStatus OriginalGroup { get; init; }

    /// <summary>The status of the original payment information blocks this report refers to (<c>OrgnlPmtInfAndSts</c>).</summary>
    public List<OriginalPaymentInformationStatus> PaymentInformations { get; init; } = [];

    /// <summary>The <c>CstmrPmtStsRpt</c> element this message was read from.</summary>
    public required XElement Source { get; init; }
}
