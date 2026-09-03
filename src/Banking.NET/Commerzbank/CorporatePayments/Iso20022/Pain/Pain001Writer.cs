using System.Text;
using System.Xml;
using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Writes pain.001 (customer credit transfer initiation) messages.</summary>
public static class Pain001Writer
{
    private const string NamespacePrefix = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.";

    /// <summary>Writes a credit transfer initiation to a new document.</summary>
    /// <param name="initiation">The credit transfer initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document. Carries an <see cref="XDeclaration"/> unless <see cref="Pain00xWriterOptions.OmitXmlDeclaration"/> is set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation, e.g. a missing payment information block, an identifier or name exceeding its maximum length, an amount not greater than zero, an invalid IBAN or BIC, or conflicting remittance information.</exception>
    public static XDocument Write(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(initiation);
        options ??= new Pain00xWriterOptions();

        PainValidator.ValidateCreditTransfer(initiation, version, options);

        var isV09 = PainWriterHelpers.IsCurrentVersion(version);
        var ns = XNamespace.Get(NamespacePrefix + (isV09 ? "09" : "03"));

        var groupHeader = new XElement(
            ns + "GrpHdr",
            new XElement(ns + "MsgId", initiation.MessageId),
            new XElement(ns + "CreDtTm", PainWriterHelpers.FormatDateTime(initiation.CreationDateTime)),
            new XElement(ns + "NbOfTxs", initiation.NumberOfTransactions),
            new XElement(ns + "CtrlSum", PainWriterHelpers.FormatAmount(initiation.ControlSum)),
            PainWriterHelpers.WriteParty(ns, "InitgPty", initiation.InitiatingParty, isV09));

        var root = new XElement(
            ns + "Document",
            new XElement(
                ns + "CstmrCdtTrfInitn",
                groupHeader,
                initiation.PaymentInformations.Select(pmtInf => WritePaymentInformation(ns, pmtInf, isV09))));

        var document = new XDocument(root);
        if (!options.OmitXmlDeclaration)
            document.Declaration = new XDeclaration("1.0", "UTF-8", null);

        return document;
    }

    /// <summary>Writes a credit transfer initiation to a UTF-8 encoded string, without a byte order mark.</summary>
    /// <param name="initiation">The credit transfer initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document as a string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static string WriteToString(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null) =>
        Encoding.UTF8.GetString(WriteToBytes(initiation, version, options));

    /// <summary>Writes a credit transfer initiation to a UTF-8 encoded byte array, without a byte order mark.</summary>
    /// <param name="initiation">The credit transfer initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document as a byte array.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static byte[] WriteToBytes(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null)
    {
        using var stream = new MemoryStream();
        Write(initiation, version, stream, options);
        return stream.ToArray();
    }

    /// <summary>Writes a credit transfer initiation to a stream, UTF-8 encoded without a byte order mark.</summary>
    /// <param name="initiation">The credit transfer initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="destination">The stream to write the document to. Not closed by this method.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static void Write(CreditTransferInitiation initiation, Pain001Version version, Stream destination, Pain00xWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        options ??= new Pain00xWriterOptions();

        var document = Write(initiation, version, options);
        PainWriterHelpers.Save(document, destination, options);
    }

    private static XElement WritePaymentInformation(XNamespace ns, CreditTransferPaymentInformation pmtInf, bool isV09)
    {
        var bicElementName = isV09 ? "BICFI" : "BIC";

        var element = new XElement(
            ns + "PmtInf",
            new XElement(ns + "PmtInfId", pmtInf.PaymentInformationId),
            new XElement(ns + "PmtMtd", "TRF"));

        if (pmtInf.BatchBooking is { } batchBooking)
            element.Add(new XElement(ns + "BtchBookg", FormatBool(batchBooking)));

        element.Add(new XElement(ns + "NbOfTxs", pmtInf.NumberOfTransactions));
        element.Add(new XElement(ns + "CtrlSum", PainWriterHelpers.FormatAmount(pmtInf.ControlSum)));

        var paymentTypeInformation = WritePaymentTypeInformation(ns, pmtInf.InstructionPriority, pmtInf.ServiceLevelCode, pmtInf.LocalInstrumentCode, pmtInf.CategoryPurposeCode);
        if (paymentTypeInformation is not null)
            element.Add(paymentTypeInformation);

        element.Add(isV09
            ? new XElement(ns + "ReqdExctnDt", new XElement(ns + "Dt", PainWriterHelpers.FormatDate(pmtInf.RequestedExecutionDate)))
            : new XElement(ns + "ReqdExctnDt", PainWriterHelpers.FormatDate(pmtInf.RequestedExecutionDate)));

        element.Add(PainWriterHelpers.WriteParty(ns, "Dbtr", pmtInf.Debtor, isV09));
        element.Add(PainWriterHelpers.WriteAccount(ns, "DbtrAcct", pmtInf.DebtorAccount, includeCurrency: true));
        element.Add(new XElement(ns + "DbtrAgt", PainWriterHelpers.WriteAgent(ns, bicElementName, pmtInf.DebtorAgent, "NOTPROVIDED")));

        if (pmtInf.UltimateDebtor is { } ultimateDebtor)
            element.Add(PainWriterHelpers.WriteParty(ns, "UltmtDbtr", ultimateDebtor, isV09));

        if (pmtInf.ChargeBearer is { } chargeBearer)
            element.Add(new XElement(ns + "ChrgBr", FormatChargeBearer(chargeBearer)));

        foreach (var transaction in pmtInf.Transactions)
            element.Add(WriteTransaction(ns, transaction, isV09));

        return element;
    }

    private static XElement WriteTransaction(XNamespace ns, CreditTransferTransaction transaction, bool isV09)
    {
        var bicElementName = isV09 ? "BICFI" : "BIC";

        var paymentId = new XElement(ns + "PmtId");
        if (transaction.InstructionId is { } instructionId)
            paymentId.Add(new XElement(ns + "InstrId", instructionId));
        paymentId.Add(new XElement(ns + "EndToEndId", transaction.EndToEndId));

        var element = new XElement(
            ns + "CdtTrfTxInf",
            paymentId,
            new XElement(
                ns + "Amt",
                new XElement(ns + "InstdAmt", new XAttribute("Ccy", transaction.Amount.Currency), PainWriterHelpers.FormatAmount(transaction.Amount.Amount))));

        if (transaction.UltimateDebtor is { } ultimateDebtor)
            element.Add(PainWriterHelpers.WriteParty(ns, "UltmtDbtr", ultimateDebtor, isV09));

        if (transaction.CreditorAgent?.Bic is { } creditorAgentBic)
            element.Add(new XElement(ns + "CdtrAgt", new XElement(ns + "FinInstnId", new XElement(ns + bicElementName, creditorAgentBic))));

        element.Add(PainWriterHelpers.WriteParty(ns, "Cdtr", transaction.Creditor, isV09));
        element.Add(PainWriterHelpers.WriteAccount(ns, "CdtrAcct", transaction.CreditorAccount, includeCurrency: false));

        if (transaction.UltimateCreditor is { } ultimateCreditor)
            element.Add(PainWriterHelpers.WriteParty(ns, "UltmtCdtr", ultimateCreditor, isV09));

        if (transaction.PurposeCode is { } purposeCode)
            element.Add(new XElement(ns + "Purp", new XElement(ns + "Cd", purposeCode)));

        if (transaction.RemittanceInformation is { } remittance && PainWriterHelpers.WriteRemittance(ns, remittance) is { } remittanceElement)
            element.Add(remittanceElement);

        return element;
    }

    private static XElement? WritePaymentTypeInformation(XNamespace ns, InstructionPriority? priority, string? serviceLevelCode, string? localInstrumentCode, string? categoryPurposeCode)
    {
        if (priority is null && serviceLevelCode is null && localInstrumentCode is null && categoryPurposeCode is null)
            return null;

        var element = new XElement(ns + "PmtTpInf");

        if (priority is { } instructionPriority)
            element.Add(new XElement(ns + "InstrPrty", instructionPriority == InstructionPriority.High ? "HIGH" : "NORM"));

        if (serviceLevelCode is { } serviceLevel)
            element.Add(new XElement(ns + "SvcLvl", new XElement(ns + "Cd", serviceLevel)));

        if (localInstrumentCode is { } localInstrument)
            element.Add(new XElement(ns + "LclInstrm", new XElement(ns + "Cd", localInstrument)));

        if (categoryPurposeCode is { } categoryPurpose)
            element.Add(new XElement(ns + "CtgyPurp", new XElement(ns + "Cd", categoryPurpose)));

        return element;
    }

    private static string FormatChargeBearer(ChargeBearer chargeBearer) => chargeBearer switch
    {
        ChargeBearer.Slev => "SLEV",
        ChargeBearer.Shar => "SHAR",
        ChargeBearer.Debt => "DEBT",
        ChargeBearer.Cred => "CRED",
        _ => throw new ArgumentOutOfRangeException(nameof(chargeBearer), chargeBearer, "Unknown charge bearer."),
    };

    private static string FormatBool(bool value) => value ? "true" : "false";
}
