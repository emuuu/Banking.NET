using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Key information about the original transaction a status refers to (ISO 20022 <c>OriginalTransactionReference</c>).</summary>
public sealed class OriginalTransactionReference
{
    /// <summary>The original instructed or equivalent amount (<c>Amt/InstdAmt</c> or <c>Amt/EqvtAmt/Amt</c>).</summary>
    public Money? Amount { get; init; }

    /// <summary>The original requested execution date (<c>ReqdExctnDt</c>; a plain date in schema version .03, or the <c>Dt</c> child of a date/date-time choice in .10).</summary>
    public DateOnly? RequestedExecutionDate { get; init; }

    /// <summary>The original requested execution date and time (<c>ReqdExctnDt/DtTm</c>, schema version .10 only). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? RequestedExecutionDateTime { get; init; }

    /// <summary>The original requested collection date, direct debits (<c>ReqdColltnDt</c>).</summary>
    public DateOnly? RequestedCollectionDate { get; init; }

    /// <summary>The original creditor scheme identification, direct debits (<c>CdtrSchmeId</c>).</summary>
    public PartyIdentification? CreditorSchemeId { get; init; }

    /// <summary>The original coded payment method (<c>PmtMtd</c>).</summary>
    public string? PaymentMethod { get; init; }

    /// <summary>The original payment type information (<c>PmtTpInf</c>).</summary>
    public PaymentTypeInformation? PaymentTypeInformation { get; init; }

    /// <summary>The original mandate-related information, direct debits (<c>MndtRltdInf</c>).</summary>
    public MandateInformation? Mandate { get; init; }

    /// <summary>The original remittance information (<c>RmtInf</c>).</summary>
    public RemittanceInformation? RemittanceInformation { get; init; }

    /// <summary>The original debtor (<c>Dbtr</c>).</summary>
    public PartyIdentification? Debtor { get; init; }

    /// <summary>The original debtor account (<c>DbtrAcct</c>).</summary>
    public AccountIdentification? DebtorAccount { get; init; }

    /// <summary>The original debtor agent (<c>DbtrAgt</c>).</summary>
    public FinancialInstitution? DebtorAgent { get; init; }

    /// <summary>The original creditor agent (<c>CdtrAgt</c>).</summary>
    public FinancialInstitution? CreditorAgent { get; init; }

    /// <summary>The original creditor (<c>Cdtr</c>).</summary>
    public PartyIdentification? Creditor { get; init; }

    /// <summary>The original creditor account (<c>CdtrAcct</c>).</summary>
    public AccountIdentification? CreditorAccount { get; init; }

    /// <summary>The original ultimate debtor (<c>UltmtDbtr</c>).</summary>
    public PartyIdentification? UltimateDebtor { get; init; }

    /// <summary>The original ultimate creditor (<c>UltmtCdtr</c>).</summary>
    public PartyIdentification? UltimateCreditor { get; init; }

    /// <summary>The original coded purpose (<c>Purp/Cd</c>).</summary>
    public string? PurposeCode { get; init; }

    /// <summary>The <c>OrgnlTxRef</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
