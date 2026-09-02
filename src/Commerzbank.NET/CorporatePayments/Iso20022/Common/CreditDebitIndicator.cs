namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Indicates whether an amount is a credit or a debit (ISO 20022 <c>CdtDbtInd</c>).</summary>
public enum CreditDebitIndicator
{
    /// <summary>The amount is a credit (<c>CRDT</c>).</summary>
    Credit,

    /// <summary>The amount is a debit (<c>DBIT</c>).</summary>
    Debit,
}
