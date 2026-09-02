namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Describes the type of a payment, e.g. its service level or local instrument (ISO 20022 <c>PaymentTypeInformation</c>).</summary>
public sealed class PaymentTypeInformation
{
    /// <summary>The coded instruction priority (<c>InstrPrty</c>).</summary>
    public string? InstructionPriority { get; set; }

    /// <summary>The coded service level, e.g. <c>SEPA</c> (<c>SvcLvl/Cd</c>).</summary>
    public string? ServiceLevelCode { get; set; }

    /// <summary>The proprietary service level (<c>SvcLvl/Prtry</c>).</summary>
    public string? ServiceLevelProprietary { get; set; }

    /// <summary>The coded local instrument, e.g. <c>CORE</c> or <c>B2B</c> (<c>LclInstrm/Cd</c>).</summary>
    public string? LocalInstrumentCode { get; set; }

    /// <summary>The proprietary local instrument (<c>LclInstrm/Prtry</c>).</summary>
    public string? LocalInstrumentProprietary { get; set; }

    /// <summary>The coded sequence type, e.g. <c>FRST</c> or <c>RCUR</c> (<c>SeqTp</c>).</summary>
    public string? SequenceType { get; set; }

    /// <summary>The coded category purpose (<c>CtgyPurp/Cd</c>).</summary>
    public string? CategoryPurposeCode { get; set; }

    /// <summary>The proprietary category purpose (<c>CtgyPurp/Prtry</c>).</summary>
    public string? CategoryPurposeProprietary { get; set; }
}
