using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Information about a returned or rejected transaction (<c>RtrInf</c>).</summary>
public sealed class ReturnInformation
{
    /// <summary>The bank transaction code of the original transaction (<c>OrgnlBkTxCd</c>).</summary>
    public BankTransactionCode? OriginalBankTransactionCode { get; init; }

    /// <summary>The party that initiated the return (<c>Orgtr</c>).</summary>
    public PartyIdentification? Originator { get; init; }

    /// <summary>The coded return reason (<c>Rsn/Cd</c>).</summary>
    public string? ReasonCode { get; init; }

    /// <summary>The proprietary return reason (<c>Rsn/Prtry</c>).</summary>
    public string? ReasonProprietary { get; init; }

    /// <summary>Additional free-form information about the return (<c>AddtlInf</c>).</summary>
    public List<string> AdditionalInformation { get; init; } = [];

    /// <summary>The <c>RtrInf</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
