namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>The requested priority of a payment instruction (ISO 20022 <c>Priority2Code</c>, <c>InstrPrty</c>).</summary>
public enum InstructionPriority
{
    /// <summary>High priority (<c>HIGH</c>).</summary>
    High,

    /// <summary>Normal priority (<c>NORM</c>).</summary>
    Normal,
}
