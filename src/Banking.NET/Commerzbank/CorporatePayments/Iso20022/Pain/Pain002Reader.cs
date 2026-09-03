using System.Globalization;
using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Reads pain.002 (customer payment status report) messages.</summary>
public static class Pain002Reader
{
    /// <summary>Reads a payment status report from an already-loaded document.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="Iso20022ValidationException">The document's root element is not named <c>Document</c>, has no <c>CstmrPmtStsRpt</c> child, that child has no <c>OrgnlGrpInfAndSts</c> child, or a non-nullable element such as <c>NbOfTxsPerSts/DtldSts</c>/<c>DtldNbOfTxs</c> is missing or not parsable.</exception>
    public static PaymentStatusReport Read(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Document")
            throw new Iso20022ValidationException(
                $"Expected root element 'Document', but found '{root?.Name.LocalName ?? "none"}'.", "Document");

        var message = root.Child("CstmrPmtStsRpt")
            ?? throw new Iso20022ValidationException(
                $"Expected root child 'CstmrPmtStsRpt', but found '{root.Elements().FirstOrDefault()?.Name.LocalName ?? "none"}'.",
                "Document/CstmrPmtStsRpt");

        var groupHeader = message.Child("GrpHdr");

        return new PaymentStatusReport
        {
            Identifier = Iso20022Document.Identify(document),
            MessageId = groupHeader.Child("MsgId").Value(),
            CreationDateTime = groupHeader.Child("CreDtTm").DateTime(),
            InitiatingParty = CommonReaders.ReadParty(groupHeader.Child("InitgPty")),
            DebtorAgent = CommonReaders.ReadFinancialInstitution(groupHeader.Child("DbtrAgt")),
            CreditorAgent = CommonReaders.ReadFinancialInstitution(groupHeader.Child("CdtrAgt")),
            OriginalGroup = ReadOriginalGroupStatus(message.Child("OrgnlGrpInfAndSts")),
            PaymentInformations = [.. message.Children("OrgnlPmtInfAndSts").Select(ReadOriginalPaymentInformationStatus)],
            Source = message,
        };
    }

    /// <summary>Reads a payment status report from a stream.</summary>
    /// <param name="xml">The stream to read the document from.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.Xml.XmlException"><paramref name="xml"/> is not well-formed XML.</exception>
    /// <exception cref="Iso20022ValidationException">The document fails structural validation. See <see cref="Read(XDocument)"/>.</exception>
    public static PaymentStatusReport Read(Stream xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a payment status report from a string.</summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ArgumentException"><paramref name="xml"/> is <see langword="null"/>, empty, or whitespace-only.</exception>
    /// <exception cref="System.Xml.XmlException"><paramref name="xml"/> is not well-formed XML.</exception>
    /// <exception cref="Iso20022ValidationException">The document fails structural validation. See <see cref="Read(XDocument)"/>.</exception>
    public static PaymentStatusReport Read(string xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a payment status report from a downloaded Corporate Payments message.</summary>
    /// <param name="message">The downloaded message.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.Xml.XmlException">The message content is not well-formed XML.</exception>
    /// <exception cref="Iso20022ValidationException">The document fails structural validation. See <see cref="Read(XDocument)"/>.</exception>
    public static PaymentStatusReport Read(CorporatePaymentsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Read(message.GetContentAsString());
    }

    private static OriginalGroupStatus ReadOriginalGroupStatus(XElement? element)
    {
        if (element is null)
            throw new Iso20022ValidationException("Missing 'OrgnlGrpInfAndSts' element.", "OrgnlGrpInfAndSts");

        return new OriginalGroupStatus
        {
            OriginalMessageId = element.Child("OrgnlMsgId").Value(),
            OriginalMessageNameId = element.Child("OrgnlMsgNmId").Value(),
            OriginalCreationDateTime = element.Child("OrgnlCreDtTm").DateTime(),
            OriginalNumberOfTransactions = element.Child("OrgnlNbOfTxs").Int(),
            OriginalControlSum = element.Child("OrgnlCtrlSum").Decimal(),
            GroupStatus = element.Child("GrpSts").Value(),
            StatusReasons = CommonReaders.ReadStatusReasons(element.Children("StsRsnInf")),
            NumberOfTransactionsPerStatus = [.. element.Children("NbOfTxsPerSts").Select(ReadTransactionCountPerStatus)],
            Source = element,
        };
    }

    private static OriginalPaymentInformationStatus ReadOriginalPaymentInformationStatus(XElement element) =>
        new()
        {
            OriginalPaymentInformationId = element.Child("OrgnlPmtInfId").Value(),
            OriginalNumberOfTransactions = element.Child("OrgnlNbOfTxs").Int(),
            OriginalControlSum = element.Child("OrgnlCtrlSum").Decimal(),
            PaymentInformationStatus = element.Child("PmtInfSts").Value(),
            StatusReasons = CommonReaders.ReadStatusReasons(element.Children("StsRsnInf")),
            NumberOfTransactionsPerStatus = [.. element.Children("NbOfTxsPerSts").Select(ReadTransactionCountPerStatus)],
            Transactions = [.. element.Children("TxInfAndSts").Select(ReadTransactionStatus)],
            Source = element,
        };

    private static TransactionStatus ReadTransactionStatus(XElement element) =>
        new()
        {
            StatusId = element.Child("StsId").Value(),
            OriginalInstructionId = element.Child("OrgnlInstrId").Value(),
            OriginalEndToEndId = element.Child("OrgnlEndToEndId").Value(),
            OriginalUetr = element.Child("OrgnlUETR").Value(),
            Status = element.Child("TxSts").Value(),
            StatusReasons = CommonReaders.ReadStatusReasons(element.Children("StsRsnInf")),
            AcceptanceDateTime = element.Child("AccptncDtTm").DateTime(),
            AccountServicerReference = element.Child("AcctSvcrRef").Value(),
            ClearingSystemReference = element.Child("ClrSysRef").Value(),
            OriginalTransaction = ReadOriginalTransactionReference(element.Child("OrgnlTxRef")),
            Source = element,
        };

    private static OriginalTransactionReference? ReadOriginalTransactionReference(XElement? element)
    {
        if (element is null)
            return null;

        var amount = element.Child("Amt");
        var requestedExecutionDate = element.Child("ReqdExctnDt");
        var mandate = element.Child("MndtRltdInf");

        return new OriginalTransactionReference
        {
            Amount = amount.Child("InstdAmt").Money() ?? amount.Path("EqvtAmt", "Amt").Money(),
            RequestedExecutionDate = requestedExecutionDate.Child("Dt").Date() ?? requestedExecutionDate.Date(),
            RequestedExecutionDateTime = requestedExecutionDate.Child("DtTm").DateTime(),
            RequestedCollectionDate = element.Child("ReqdColltnDt").Date(),
            CreditorSchemeId = CommonReaders.ReadParty(element.Child("CdtrSchmeId")),
            PaymentMethod = element.Child("PmtMtd").Value(),
            PaymentTypeInformation = CommonReaders.ReadPaymentTypeInformation(element.Child("PmtTpInf")),
            Mandate = CommonReaders.ReadMandate(mandate),
            RemittanceInformation = CommonReaders.ReadRemittance(element.Child("RmtInf")),
            Debtor = CommonReaders.ReadParty(element.Child("Dbtr")),
            DebtorAccount = CommonReaders.ReadAccount(element.Child("DbtrAcct")),
            DebtorAgent = CommonReaders.ReadFinancialInstitution(element.Child("DbtrAgt")),
            CreditorAgent = CommonReaders.ReadFinancialInstitution(element.Child("CdtrAgt")),
            Creditor = CommonReaders.ReadParty(element.Child("Cdtr")),
            CreditorAccount = CommonReaders.ReadAccount(element.Child("CdtrAcct")),
            UltimateDebtor = CommonReaders.ReadParty(element.Child("UltmtDbtr")),
            UltimateCreditor = CommonReaders.ReadParty(element.Child("UltmtCdtr")),
            PurposeCode = element.Path("Purp", "Cd").Value(),
            Source = element,
        };
    }

    private static TransactionCountPerStatus ReadTransactionCountPerStatus(XElement element)
    {
        var status = element.Child("DtldSts").Value()
            ?? throw new Iso20022ValidationException("Missing 'DtldSts' element.", "NbOfTxsPerSts/DtldSts");
        var countText = element.Child("DtldNbOfTxs").Value()
            ?? throw new Iso20022ValidationException("Missing 'DtldNbOfTxs' element.", "NbOfTxsPerSts/DtldNbOfTxs");

        if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            throw new Iso20022ValidationException($"The 'DtldNbOfTxs' value '{countText}' is not a valid integer.", "NbOfTxsPerSts/DtldNbOfTxs");

        return new TransactionCountPerStatus(status, count, element.Child("DtldCtrlSum").Decimal());
    }
}
