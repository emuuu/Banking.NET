namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A single direct debit collection within a <see cref="DirectDebitPaymentInformation"/> batch (ISO 20022 <c>DrctDbtTxInf</c>).</summary>
public sealed class DirectDebitTransaction
{
    /// <summary>The instruction identifier (<c>PmtId/InstrId</c>).</summary>
    public string? InstructionId { get; set; }

    /// <summary>The end-to-end identifier (<c>PmtId/EndToEndId</c>). At most 35 characters.</summary>
    public required string EndToEndId { get; set; }

    /// <summary>The instructed amount and currency (<c>InstdAmt</c>). Must be greater than zero, with at most two decimal places.</summary>
    public required Money Amount { get; set; }

    /// <summary>The mandate authorizing this collection (<c>DrctDbtTx/MndtRltdInf</c>).</summary>
    public required MandateInformation Mandate { get; set; }

    /// <summary>The debtor's agent (<c>DbtrAgt</c>). Written as <c>Othr/Id=NOTPROVIDED</c> when no BIC is set.</summary>
    public FinancialInstitution? DebtorAgent { get; set; }

    /// <summary>The debtor (<c>Dbtr</c>). The name is at most 70 characters in schema version .02 and 140 in .08.</summary>
    public required PartyIdentification Debtor { get; set; }

    /// <summary>The debtor's account (<c>DbtrAcct</c>). The IBAN, once normalized, must be a valid IBAN.</summary>
    public required AccountIdentification DebtorAccount { get; set; }

    /// <summary>The ultimate debtor, when different from the debtor (<c>UltmtDbtr</c>).</summary>
    public PartyIdentification? UltimateDebtor { get; set; }

    /// <summary>The coded purpose of the collection (<c>Purp/Cd</c>).</summary>
    public string? PurposeCode { get; set; }

    /// <summary>The remittance information (<c>RmtInf</c>).</summary>
    public RemittanceInformation? RemittanceInformation { get; set; }
}
