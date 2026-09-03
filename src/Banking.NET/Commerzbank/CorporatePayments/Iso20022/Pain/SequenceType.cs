namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>The sequence type of a direct debit collection within a mandate (ISO 20022 <c>SequenceType3Code</c>, <c>SeqTp</c>).</summary>
public enum SequenceType
{
    /// <summary>The first collection under a mandate (<c>FRST</c>).</summary>
    First,

    /// <summary>A recurring collection under a mandate (<c>RCUR</c>).</summary>
    Recurring,

    /// <summary>A one-off collection under a mandate (<c>OOFF</c>).</summary>
    OneOff,

    /// <summary>The final collection under a mandate (<c>FNAL</c>).</summary>
    Final,
}
