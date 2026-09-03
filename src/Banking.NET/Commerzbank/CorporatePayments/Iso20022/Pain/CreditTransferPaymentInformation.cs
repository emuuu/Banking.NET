namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A batch of credit transfer transactions sharing execution date, debtor and settlement instructions (ISO 20022 <c>PmtInf</c>).</summary>
public sealed class CreditTransferPaymentInformation
{
    /// <summary>The payment information identifier (<c>PmtInfId</c>). At most 35 characters.</summary>
    public required string PaymentInformationId { get; set; }

    /// <summary>Whether transactions should be booked as a single batch entry (<c>BtchBookg</c>).</summary>
    public bool? BatchBooking { get; set; }

    /// <summary>The requested instruction priority (<c>PmtTpInf/InstrPrty</c>).</summary>
    public InstructionPriority? InstructionPriority { get; set; }

    /// <summary>The coded service level (<c>PmtTpInf/SvcLvl/Cd</c>). Defaults to <c>"SEPA"</c>.</summary>
    public string? ServiceLevelCode { get; set; } = "SEPA";

    /// <summary>The coded local instrument (<c>PmtTpInf/LclInstrm/Cd</c>).</summary>
    public string? LocalInstrumentCode { get; set; }

    /// <summary>The coded category purpose (<c>PmtTpInf/CtgyPurp/Cd</c>).</summary>
    public string? CategoryPurposeCode { get; set; }

    /// <summary>The requested execution date (<c>ReqdExctnDt</c>).</summary>
    public required DateOnly RequestedExecutionDate { get; set; }

    /// <summary>The debtor (<c>Dbtr</c>). The name is at most 70 characters in schema version .03 and 140 in .09.</summary>
    public required PartyIdentification Debtor { get; set; }

    /// <summary>The debtor's account (<c>DbtrAcct</c>). The IBAN, once normalized, must be a valid IBAN.</summary>
    public required AccountIdentification DebtorAccount { get; set; }

    /// <summary>The debtor's agent (<c>DbtrAgt</c>). Written as <c>Othr/Id=NOTPROVIDED</c> when no BIC is set.</summary>
    public FinancialInstitution? DebtorAgent { get; set; }

    /// <summary>The ultimate debtor, when different from the debtor (<c>UltmtDbtr</c>).</summary>
    public PartyIdentification? UltimateDebtor { get; set; }

    /// <summary>Which party bears the transaction charges (<c>ChrgBr</c>). Defaults to <see cref="Iso20022.ChargeBearer.Slev"/>.</summary>
    public ChargeBearer? ChargeBearer { get; set; } = Iso20022.ChargeBearer.Slev;

    /// <summary>The individual transactions in this batch (<c>CdtTrfTxInf</c>). At least one is required.</summary>
    public List<CreditTransferTransaction> Transactions { get; set; } = [];

    /// <summary>The number of transactions in this batch, computed from <see cref="Transactions"/> (<c>NbOfTxs</c>).</summary>
    public int NumberOfTransactions => Transactions.Count;

    /// <summary>The total of the amounts of all transactions in this batch, computed from <see cref="Transactions"/> (<c>CtrlSum</c>).</summary>
    public decimal ControlSum => Transactions.Sum(t => t.Amount.Amount);
}
