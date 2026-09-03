namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A single credit transfer transaction within a <see cref="CreditTransferPaymentInformation"/> batch (ISO 20022 <c>CdtTrfTxInf</c>).</summary>
public sealed class CreditTransferTransaction
{
    /// <summary>The instruction identifier (<c>PmtId/InstrId</c>).</summary>
    public string? InstructionId { get; set; }

    /// <summary>The end-to-end identifier (<c>PmtId/EndToEndId</c>). At most 35 characters.</summary>
    public required string EndToEndId { get; set; }

    /// <summary>The instructed amount and currency (<c>Amt/InstdAmt</c>). Must be greater than zero, with at most two decimal places.</summary>
    public required Money Amount { get; set; }

    /// <summary>The ultimate debtor, when different from the debtor (<c>UltmtDbtr</c>).</summary>
    public PartyIdentification? UltimateDebtor { get; set; }

    /// <summary>The creditor's agent (<c>CdtrAgt</c>).</summary>
    public FinancialInstitution? CreditorAgent { get; set; }

    /// <summary>The creditor (<c>Cdtr</c>).</summary>
    public required PartyIdentification Creditor { get; set; }

    /// <summary>The creditor's account (<c>CdtrAcct</c>).</summary>
    public required AccountIdentification CreditorAccount { get; set; }

    /// <summary>The ultimate creditor, when different from the creditor (<c>UltmtCdtr</c>).</summary>
    public PartyIdentification? UltimateCreditor { get; set; }

    /// <summary>The coded purpose of the transfer (<c>Purp/Cd</c>).</summary>
    public string? PurposeCode { get; set; }

    /// <summary>The remittance information (<c>RmtInf</c>).</summary>
    public RemittanceInformation? RemittanceInformation { get; set; }
}
