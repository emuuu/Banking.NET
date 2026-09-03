namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A pain.008 customer direct debit initiation message, ready to be written by <see cref="Pain008Writer"/>.</summary>
public sealed class DirectDebitInitiation
{
    /// <summary>The message identifier (<c>GrpHdr/MsgId</c>). At most 35 characters.</summary>
    public required string MessageId { get; set; }

    /// <summary>The message creation date and time (<c>GrpHdr/CreDtTm</c>). Defaults to <see cref="DateTimeOffset.Now"/> at construction.</summary>
    public DateTimeOffset CreationDateTime { get; set; } = DateTimeOffset.Now;

    /// <summary>The party that initiated the message (<c>GrpHdr/InitgPty</c>). The name is at most 70 characters in schema version .02 and 140 in .08.</summary>
    public required PartyIdentification InitiatingParty { get; set; }

    /// <summary>The payment information batches (<c>PmtInf</c>). At least one is required, and each must carry at least one transaction.</summary>
    public List<DirectDebitPaymentInformation> PaymentInformations { get; set; } = [];

    /// <summary>The total number of transactions across all payment information batches, computed from <see cref="PaymentInformations"/> (<c>GrpHdr/NbOfTxs</c>).</summary>
    public int NumberOfTransactions => PaymentInformations.Sum(p => p.NumberOfTransactions);

    /// <summary>The total of the amounts of all transactions across all payment information batches, computed from <see cref="PaymentInformations"/> (<c>GrpHdr/CtrlSum</c>).</summary>
    public decimal ControlSum => PaymentInformations.Sum(p => p.ControlSum);
}
