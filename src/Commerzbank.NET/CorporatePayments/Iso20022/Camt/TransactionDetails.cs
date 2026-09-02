using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The details of a single transaction underlying a statement entry (<c>TxDtls</c>).</summary>
public sealed class TransactionDetails
{
    /// <summary>The transaction's reference identifiers (<c>Refs</c>).</summary>
    public required TransactionReferences References { get; init; }

    /// <summary>The transaction amount (<c>Amt</c>, schema version .08 only).</summary>
    public Money? Amount { get; init; }

    /// <summary>Whether the transaction is a credit or a debit (<c>CdtDbtInd</c>, schema version .08 only).</summary>
    public CreditDebitIndicator? CreditDebit { get; init; }

    /// <summary>Alternative amount representations (<c>AmtDtls</c>).</summary>
    public AmountDetails? AmountDetails { get; init; }

    /// <summary>The bank transaction code classifying the transaction (<c>BkTxCd</c>).</summary>
    public BankTransactionCode? BankTransactionCode { get; init; }

    /// <summary>The debtor (<c>RltdPties/Dbtr</c>).</summary>
    public PartyIdentification? Debtor { get; init; }

    /// <summary>The debtor's account (<c>RltdPties/DbtrAcct</c>).</summary>
    public AccountIdentification? DebtorAccount { get; init; }

    /// <summary>The ultimate debtor (<c>RltdPties/UltmtDbtr</c>).</summary>
    public PartyIdentification? UltimateDebtor { get; init; }

    /// <summary>The creditor (<c>RltdPties/Cdtr</c>).</summary>
    public PartyIdentification? Creditor { get; init; }

    /// <summary>The creditor's account (<c>RltdPties/CdtrAcct</c>).</summary>
    public AccountIdentification? CreditorAccount { get; init; }

    /// <summary>The ultimate creditor (<c>RltdPties/UltmtCdtr</c>).</summary>
    public PartyIdentification? UltimateCreditor { get; init; }

    /// <summary>The debtor's agent (<c>RltdAgts/DbtrAgt</c>).</summary>
    public FinancialInstitution? DebtorAgent { get; init; }

    /// <summary>The creditor's agent (<c>RltdAgts/CdtrAgt</c>).</summary>
    public FinancialInstitution? CreditorAgent { get; init; }

    /// <summary>The coded purpose (<c>Purp/Cd</c>).</summary>
    public string? PurposeCode { get; init; }

    /// <summary>The proprietary purpose (<c>Purp/Prtry</c>).</summary>
    public string? PurposeProprietary { get; init; }

    /// <summary>The remittance information (<c>RmtInf</c>).</summary>
    public RemittanceInformation? RemittanceInformation { get; init; }

    /// <summary>The acceptance date and time (<c>RltdDts/AccptncDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? AcceptanceDateTime { get; init; }

    /// <summary>Return information, present when the transaction is a returned or rejected payment (<c>RtrInf</c>).</summary>
    public ReturnInformation? ReturnInformation { get; init; }

    /// <summary>Additional free-form information (<c>AddtlTxInf</c>).</summary>
    public string? AdditionalTransactionInformation { get; init; }

    /// <summary>The <c>TxDtls</c> element this was read from.</summary>
    public required XElement Source { get; init; }
}
