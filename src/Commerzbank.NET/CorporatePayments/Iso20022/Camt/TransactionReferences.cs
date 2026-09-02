using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The reference identifiers of a transaction (<c>Refs</c>).</summary>
public sealed class TransactionReferences
{
    /// <summary>The message identifier (<c>MsgId</c>).</summary>
    public string? MessageId { get; init; }

    /// <summary>The reference assigned by the account servicer (<c>AcctSvcrRef</c>).</summary>
    public string? AccountServicerReference { get; init; }

    /// <summary>The payment information identifier (<c>PmtInfId</c>).</summary>
    public string? PaymentInformationId { get; init; }

    /// <summary>The instruction identifier (<c>InstrId</c>).</summary>
    public string? InstructionId { get; init; }

    /// <summary>The end-to-end identifier (<c>EndToEndId</c>).</summary>
    public string? EndToEndId { get; init; }

    /// <summary>The transaction identifier (<c>TxId</c>).</summary>
    public string? TransactionId { get; init; }

    /// <summary>The unique end-to-end transaction reference (<c>UETR</c>, schema version .08 only).</summary>
    public string? Uetr { get; init; }

    /// <summary>The mandate identifier (<c>MndtId</c>).</summary>
    public string? MandateId { get; init; }

    /// <summary>The cheque number (<c>ChqNb</c>).</summary>
    public string? ChequeNumber { get; init; }

    /// <summary>The clearing system reference (<c>ClrSysRef</c>).</summary>
    public string? ClearingSystemReference { get; init; }

    /// <summary>Proprietary references, as (type, reference) pairs (<c>Prtry</c>).</summary>
    public List<(string Type, string Reference)> Proprietary { get; init; } = [];

    /// <summary>The <c>Refs</c> element this was read from, or <see langword="null"/> when the transaction had no <c>Refs</c> element.</summary>
    public XElement? Source { get; init; }
}
