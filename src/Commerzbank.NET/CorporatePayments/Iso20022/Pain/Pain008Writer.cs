using System.Text;
using System.Xml.Linq;
using Commerzbank.NET.CorporatePayments.Iso20022.Internal;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Writes pain.008 (customer direct debit initiation) messages.</summary>
public static class Pain008Writer
{
    private const string NamespacePrefix = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.";

    /// <summary>Writes a direct debit initiation to a new document.</summary>
    /// <param name="initiation">The direct debit initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document. Carries an <see cref="XDeclaration"/> unless <see cref="Pain00xWriterOptions.OmitXmlDeclaration"/> is set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation, e.g. a missing payment information block, an identifier or name exceeding its maximum length, an amount not greater than zero, an invalid IBAN or BIC, a missing mandate date of signature, or conflicting remittance information.</exception>
    public static XDocument Write(DirectDebitInitiation initiation, Pain008Version version, Pain00xWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(initiation);
        options ??= new Pain00xWriterOptions();

        PainValidator.ValidateDirectDebit(initiation, version, options);

        var isV08 = PainWriterHelpers.IsCurrentVersion(version);
        var ns = XNamespace.Get(NamespacePrefix + (isV08 ? "08" : "02"));

        var groupHeader = new XElement(
            ns + "GrpHdr",
            new XElement(ns + "MsgId", initiation.MessageId),
            new XElement(ns + "CreDtTm", PainWriterHelpers.FormatDateTime(initiation.CreationDateTime)),
            new XElement(ns + "NbOfTxs", initiation.NumberOfTransactions),
            new XElement(ns + "CtrlSum", PainWriterHelpers.FormatAmount(initiation.ControlSum)),
            PainWriterHelpers.WriteParty(ns, "InitgPty", initiation.InitiatingParty, isV08));

        var root = new XElement(
            ns + "Document",
            new XElement(
                ns + "CstmrDrctDbtInitn",
                groupHeader,
                initiation.PaymentInformations.Select(pmtInf => WritePaymentInformation(ns, pmtInf, isV08))));

        var document = new XDocument(root);
        if (!options.OmitXmlDeclaration)
            document.Declaration = new XDeclaration("1.0", "UTF-8", null);

        return document;
    }

    /// <summary>Writes a direct debit initiation to a UTF-8 encoded string, without a byte order mark.</summary>
    /// <param name="initiation">The direct debit initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document as a string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static string WriteToString(DirectDebitInitiation initiation, Pain008Version version, Pain00xWriterOptions? options = null) =>
        Encoding.UTF8.GetString(WriteToBytes(initiation, version, options));

    /// <summary>Writes a direct debit initiation to a UTF-8 encoded byte array, without a byte order mark.</summary>
    /// <param name="initiation">The direct debit initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <returns>The written document as a byte array.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static byte[] WriteToBytes(DirectDebitInitiation initiation, Pain008Version version, Pain00xWriterOptions? options = null)
    {
        using var stream = new MemoryStream();
        Write(initiation, version, stream, options);
        return stream.ToArray();
    }

    /// <summary>Writes a direct debit initiation to a stream, UTF-8 encoded without a byte order mark.</summary>
    /// <param name="initiation">The direct debit initiation to write.</param>
    /// <param name="version">The schema version to write.</param>
    /// <param name="destination">The stream to write the document to. Not closed by this method.</param>
    /// <param name="options">Options controlling how the document is rendered, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="initiation"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    /// <exception cref="Iso20022ValidationException">The initiation fails validation.</exception>
    public static void Write(DirectDebitInitiation initiation, Pain008Version version, Stream destination, Pain00xWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        options ??= new Pain00xWriterOptions();

        var document = Write(initiation, version, options);
        PainWriterHelpers.Save(document, destination, options);
    }

    private static XElement WritePaymentInformation(XNamespace ns, DirectDebitPaymentInformation pmtInf, bool isV08)
    {
        var bicElementName = isV08 ? "BICFI" : "BIC";

        var element = new XElement(
            ns + "PmtInf",
            new XElement(ns + "PmtInfId", pmtInf.PaymentInformationId),
            new XElement(ns + "PmtMtd", "DD"));

        if (pmtInf.BatchBooking is { } batchBooking)
            element.Add(new XElement(ns + "BtchBookg", FormatBool(batchBooking)));

        element.Add(new XElement(ns + "NbOfTxs", pmtInf.NumberOfTransactions));
        element.Add(new XElement(ns + "CtrlSum", PainWriterHelpers.FormatAmount(pmtInf.ControlSum)));

        var paymentTypeInformation = new XElement(
            ns + "PmtTpInf",
            new XElement(ns + "SvcLvl", new XElement(ns + "Cd", pmtInf.ServiceLevelCode ?? "SEPA")),
            new XElement(ns + "LclInstrm", new XElement(ns + "Cd", FormatScheme(pmtInf.Scheme))),
            new XElement(ns + "SeqTp", FormatSequenceType(pmtInf.SequenceType)));
        if (pmtInf.CategoryPurposeCode is { } categoryPurpose)
            paymentTypeInformation.Add(new XElement(ns + "CtgyPurp", new XElement(ns + "Cd", categoryPurpose)));
        element.Add(paymentTypeInformation);

        element.Add(new XElement(ns + "ReqdColltnDt", PainWriterHelpers.FormatDate(pmtInf.RequestedCollectionDate)));
        element.Add(PainWriterHelpers.WriteParty(ns, "Cdtr", pmtInf.Creditor, isV08));
        element.Add(PainWriterHelpers.WriteAccount(ns, "CdtrAcct", pmtInf.CreditorAccount, includeCurrency: false));
        element.Add(new XElement(ns + "CdtrAgt", PainWriterHelpers.WriteAgent(ns, bicElementName, pmtInf.CreditorAgent, "NOTPROVIDED")));

        if (pmtInf.UltimateCreditor is { } ultimateCreditor)
            element.Add(PainWriterHelpers.WriteParty(ns, "UltmtCdtr", ultimateCreditor, isV08));

        if (pmtInf.ChargeBearer is { } chargeBearer)
            element.Add(new XElement(ns + "ChrgBr", FormatChargeBearer(chargeBearer)));

        element.Add(new XElement(
            ns + "CdtrSchmeId",
            new XElement(
                ns + "Id",
                new XElement(
                    ns + "PrvtId",
                    new XElement(
                        ns + "Othr",
                        new XElement(ns + "Id", pmtInf.CreditorSchemeId),
                        new XElement(ns + "SchmeNm", new XElement(ns + "Prtry", "SEPA")))))));

        foreach (var transaction in pmtInf.Transactions)
            element.Add(WriteTransaction(ns, transaction, isV08));

        return element;
    }

    private static XElement WriteTransaction(XNamespace ns, DirectDebitTransaction transaction, bool isV08)
    {
        var bicElementName = isV08 ? "BICFI" : "BIC";

        var paymentId = new XElement(ns + "PmtId");
        if (transaction.InstructionId is { } instructionId)
            paymentId.Add(new XElement(ns + "InstrId", instructionId));
        paymentId.Add(new XElement(ns + "EndToEndId", transaction.EndToEndId));

        var element = new XElement(
            ns + "DrctDbtTxInf",
            paymentId,
            new XElement(ns + "InstdAmt", new XAttribute("Ccy", transaction.Amount.Currency), PainWriterHelpers.FormatAmount(transaction.Amount.Amount)),
            new XElement(ns + "DrctDbtTx", WriteMandate(ns, transaction.Mandate, bicElementName)),
            new XElement(ns + "DbtrAgt", PainWriterHelpers.WriteAgent(ns, bicElementName, transaction.DebtorAgent, "NOTPROVIDED")),
            PainWriterHelpers.WriteParty(ns, "Dbtr", transaction.Debtor, isV08),
            PainWriterHelpers.WriteAccount(ns, "DbtrAcct", transaction.DebtorAccount, includeCurrency: false));

        if (transaction.UltimateDebtor is { } ultimateDebtor)
            element.Add(PainWriterHelpers.WriteParty(ns, "UltmtDbtr", ultimateDebtor, isV08));

        if (transaction.PurposeCode is { } purposeCode)
            element.Add(new XElement(ns + "Purp", new XElement(ns + "Cd", purposeCode)));

        if (transaction.RemittanceInformation is { } remittance && PainWriterHelpers.WriteRemittance(ns, remittance) is { } remittanceElement)
            element.Add(remittanceElement);

        return element;
    }

    private static XElement WriteMandate(XNamespace ns, MandateInformation mandate, string bicElementName)
    {
        var element = new XElement(
            ns + "MndtRltdInf",
            new XElement(ns + "MndtId", mandate.MandateId),
            new XElement(ns + "DtOfSgntr", PainWriterHelpers.FormatDate(mandate.DateOfSignature!.Value)));

        if (mandate.AmendmentIndicator is { } amendmentIndicator)
        {
            element.Add(new XElement(ns + "AmdmntInd", FormatBool(amendmentIndicator)));

            if (amendmentIndicator)
                element.Add(WriteAmendmentDetails(ns, mandate, bicElementName));
        }

        return element;
    }

    private static XElement WriteAmendmentDetails(XNamespace ns, MandateInformation mandate, string bicElementName)
    {
        var element = new XElement(ns + "AmdmntInfDtls");

        if (mandate.OriginalMandateId is { } originalMandateId)
            element.Add(new XElement(ns + "OrgnlMndtId", originalMandateId));

        if (mandate.OriginalCreditorName is not null || mandate.OriginalCreditorSchemeId is not null)
        {
            var originalCreditorSchemeId = new XElement(ns + "OrgnlCdtrSchmeId");
            if (mandate.OriginalCreditorName is { } originalCreditorName)
                originalCreditorSchemeId.Add(new XElement(ns + "Nm", originalCreditorName));
            if (mandate.OriginalCreditorSchemeId is { } originalSchemeId)
            {
                originalCreditorSchemeId.Add(new XElement(
                    ns + "Id",
                    new XElement(
                        ns + "PrvtId",
                        new XElement(
                            ns + "Othr",
                            new XElement(ns + "Id", originalSchemeId),
                            new XElement(ns + "SchmeNm", new XElement(ns + "Prtry", "SEPA"))))));
            }

            element.Add(originalCreditorSchemeId);
        }

        if (mandate.OriginalDebtorAccount is { } originalDebtorAccount)
            element.Add(PainWriterHelpers.WriteAccount(ns, "OrgnlDbtrAcct", originalDebtorAccount, includeCurrency: false));

        if (mandate.OriginalDebtorAgent is { } originalDebtorAgent)
            element.Add(new XElement(ns + "OrgnlDbtrAgt", PainWriterHelpers.WriteAgent(ns, bicElementName, originalDebtorAgent, "SMNDA")));

        return element;
    }

    private static string FormatScheme(DirectDebitScheme scheme) => scheme switch
    {
        DirectDebitScheme.Core => "CORE",
        DirectDebitScheme.B2B => "B2B",
        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unknown direct debit scheme."),
    };

    private static string FormatSequenceType(SequenceType sequenceType) => sequenceType switch
    {
        SequenceType.First => "FRST",
        SequenceType.Recurring => "RCUR",
        SequenceType.OneOff => "OOFF",
        SequenceType.Final => "FNAL",
        _ => throw new ArgumentOutOfRangeException(nameof(sequenceType), sequenceType, "Unknown sequence type."),
    };

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
