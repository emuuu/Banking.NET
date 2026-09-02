using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Alternative amount representations of a transaction (<c>AmtDtls</c>).</summary>
public sealed class AmountDetails
{
    /// <summary>The amount as instructed by the initiating party (<c>InstdAmt/Amt</c>).</summary>
    public Money? InstructedAmount { get; init; }

    /// <summary>The amount before deducting charges (<c>TxAmt/Amt</c>).</summary>
    public Money? TransactionAmount { get; init; }

    /// <summary>The counter-value amount in a currency exchange (<c>CntrValAmt/Amt</c>).</summary>
    public Money? CounterValueAmount { get; init; }

    /// <summary>The exchange rate applied (<c>CntrValAmt/CcyXchg/XchgRate</c>).</summary>
    public decimal? ExchangeRate { get; init; }

    /// <summary>The <c>AmtDtls</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
