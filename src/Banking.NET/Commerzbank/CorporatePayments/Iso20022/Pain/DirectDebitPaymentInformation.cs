namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A batch of direct debit collections sharing collection date, creditor and mandate scheme (ISO 20022 <c>PmtInf</c>).</summary>
public sealed class DirectDebitPaymentInformation
{
    /// <summary>The payment information identifier (<c>PmtInfId</c>). At most 35 characters.</summary>
    public required string PaymentInformationId { get; set; }

    /// <summary>Whether transactions should be booked as a single batch entry (<c>BtchBookg</c>).</summary>
    public bool? BatchBooking { get; set; }

    /// <summary>The coded service level (<c>PmtTpInf/SvcLvl/Cd</c>). Defaults to <c>"SEPA"</c>.</summary>
    public string? ServiceLevelCode { get; set; } = "SEPA";

    /// <summary>The direct debit scheme (<c>PmtTpInf/LclInstrm/Cd</c>).</summary>
    public required DirectDebitScheme Scheme { get; set; }

    /// <summary>The sequence type of this collection within the mandate (<c>PmtTpInf/SeqTp</c>).</summary>
    public required SequenceType SequenceType { get; set; }

    /// <summary>The coded category purpose (<c>PmtTpInf/CtgyPurp/Cd</c>).</summary>
    public string? CategoryPurposeCode { get; set; }

    /// <summary>The requested collection date (<c>ReqdColltnDt</c>).</summary>
    public required DateOnly RequestedCollectionDate { get; set; }

    /// <summary>The creditor (<c>Cdtr</c>). The name is at most 70 characters in schema version .02 and 140 in .08.</summary>
    public required PartyIdentification Creditor { get; set; }

    /// <summary>The creditor's account (<c>CdtrAcct</c>). The IBAN, once normalized, must be a valid IBAN.</summary>
    public required AccountIdentification CreditorAccount { get; set; }

    /// <summary>The creditor's agent (<c>CdtrAgt</c>). Written as <c>Othr/Id=NOTPROVIDED</c> when no BIC is set.</summary>
    public FinancialInstitution? CreditorAgent { get; set; }

    /// <summary>The ultimate creditor, when different from the creditor (<c>UltmtCdtr</c>).</summary>
    public PartyIdentification? UltimateCreditor { get; set; }

    /// <summary>Which party bears the transaction charges (<c>ChrgBr</c>). Defaults to <see cref="Iso20022.ChargeBearer.Slev"/>.</summary>
    public ChargeBearer? ChargeBearer { get; set; } = Iso20022.ChargeBearer.Slev;

    /// <summary>The SEPA creditor identifier (<c>CdtrSchmeId/Id/PrvtId/Othr/Id</c>). At most 35 characters.</summary>
    public required string CreditorSchemeId { get; set; }

    /// <summary>The individual collections in this batch (<c>DrctDbtTxInf</c>). At least one is required.</summary>
    public List<DirectDebitTransaction> Transactions { get; set; } = [];

    /// <summary>The number of transactions in this batch, computed from <see cref="Transactions"/> (<c>NbOfTxs</c>).</summary>
    public int NumberOfTransactions => Transactions.Count;

    /// <summary>The total of the amounts of all transactions in this batch, computed from <see cref="Transactions"/> (<c>CtrlSum</c>).</summary>
    public decimal ControlSum => Transactions.Sum(t => t.Amount.Amount);
}
