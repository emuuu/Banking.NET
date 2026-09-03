using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Reads camt.052 (account report), camt.053 (account statement) and camt.054 (debit/credit notification) messages.</summary>
public static class CamtReader
{
    /// <summary>Reads a bank-to-customer message from an already-loaded document.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The message.</returns>
    /// <exception cref="Iso20022ValidationException">The document's root element is not named <c>Document</c>, or has none of the expected children <c>BkToCstmrAcctRpt</c>, <c>BkToCstmrStmt</c> or <c>BkToCstmrDbtCdtNtfctn</c>.</exception>
    public static BankToCustomerMessage Read(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Document")
            throw new Iso20022ValidationException(
                $"Expected root element 'Document', but found '{root?.Name.LocalName ?? "none"}'.", "Document");

        var (kind, message, entryContainer) = root.Child("BkToCstmrAcctRpt") is { } acctRpt
            ? (StatementKind.Report, acctRpt, "Rpt")
            : root.Child("BkToCstmrStmt") is { } stmt
            ? (StatementKind.Statement, stmt, "Stmt")
            : root.Child("BkToCstmrDbtCdtNtfctn") is { } ntfctn
            ? (StatementKind.Notification, ntfctn, "Ntfctn")
            : throw new Iso20022ValidationException(
                $"Expected root child 'BkToCstmrAcctRpt', 'BkToCstmrStmt' or 'BkToCstmrDbtCdtNtfctn', but found '{root.Elements().FirstOrDefault()?.Name.LocalName ?? "none"}'.",
                MissingMessageElementPath(document));

        return new BankToCustomerMessage
        {
            Identifier = Iso20022Document.Identify(document),
            GroupHeader = ReadGroupHeader(message.Child("GrpHdr")),
            Statements = [.. message.Children(entryContainer).Select(e => ReadStatement(e, kind))],
            Source = message,
        };
    }

    /// <summary>Determines the message-element path to report when the expected bank-to-customer child is missing, based on the message type identified from the document's root namespace.</summary>
    private static string MissingMessageElementPath(XDocument document) => Iso20022Document.Identify(document).Type switch
    {
        Iso20022MessageType.Camt052 => "Document/BkToCstmrAcctRpt",
        Iso20022MessageType.Camt053 => "Document/BkToCstmrStmt",
        Iso20022MessageType.Camt054 => "Document/BkToCstmrDbtCdtNtfctn",
        _ => "Document",
    };

    /// <summary>Reads a bank-to-customer message from a stream.</summary>
    /// <param name="xml">The stream to read the document from.</param>
    /// <returns>The message.</returns>
    public static BankToCustomerMessage Read(Stream xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a bank-to-customer message from a string.</summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>The message.</returns>
    public static BankToCustomerMessage Read(string xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a bank-to-customer message from a downloaded Corporate Payments message.</summary>
    /// <param name="message">The downloaded message.</param>
    /// <returns>The message.</returns>
    public static BankToCustomerMessage Read(CorporatePaymentsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Read(message.GetContentAsString());
    }

    private static GroupHeader ReadGroupHeader(XElement? element)
    {
        if (element is null)
            throw new Iso20022ValidationException("Missing 'GrpHdr' element.", "GrpHdr");

        return new GroupHeader
        {
            MessageId = element.Child("MsgId").Value(),
            CreationDateTime = element.Child("CreDtTm").DateTime(),
            MessageRecipientName = element.Path("MsgRcpt", "Nm").Value(),
            PageNumber = element.Path("MsgPgntn", "PgNb").Int(),
            LastPageIndicator = element.Path("MsgPgntn", "LastPgInd").Bool(),
            AdditionalInformation = element.Child("AddtlInf").Value(),
            Source = element,
        };
    }

    private static AccountStatement ReadStatement(XElement element, StatementKind kind)
    {
        var account = CommonReaders.ReadAccount(element.Child("Acct"));
        var statementCurrency = account?.Currency;

        return new AccountStatement
        {
            Kind = kind,
            Id = element.Child("Id").Value(),
            ElectronicSequenceNumber = element.Child("ElctrncSeqNb").Long(),
            LegalSequenceNumber = element.Child("LglSeqNb").Long(),
            CreationDateTime = element.Child("CreDtTm").DateTime(),
            FromDateTime = element.Path("FrToDt", "FrDtTm").DateTime(),
            ToDateTime = element.Path("FrToDt", "ToDtTm").DateTime(),
            CopyDuplicateIndicator = element.Child("CpyDplctInd").Value(),
            Account = account,
            AccountOwnerName = element.Path("Acct", "Ownr", "Nm").Value(),
            AccountServicer = CommonReaders.ReadFinancialInstitution(element.Path("Acct", "Svcr")),
            Balances = [.. element.Children("Bal").Select(e => ReadBalance(e, statementCurrency))],
            TransactionsSummary = ReadTransactionsSummary(element.Child("TxsSummry"), statementCurrency),
            Entries = [.. element.Children("Ntry").Select(e => ReadEntry(e, statementCurrency))],
            AdditionalStatementInformation = element.Child("AddtlStmtInf").Value(),
            Source = element,
        };
    }

    private static Balance ReadBalance(XElement element, string? statementCurrency)
    {
        var typeElement = element.Path("Tp", "CdOrPrtry");

        return new Balance
        {
            TypeCode = typeElement.Child("Cd").Value(),
            TypeProprietary = typeElement.Child("Prtry").Value(),
            SubTypeCode = element.Path("Tp", "SubTp", "Cd").Value(),
            Amount = ReadMoney(element.Child("Amt"), statementCurrency)
                ?? throw new Iso20022ValidationException("Missing 'Amt' element on balance.", "Bal/Amt"),
            CreditDebit = CommonReaders.ReadCreditDebit(element.Child("CdtDbtInd"))
                ?? throw new Iso20022ValidationException("Missing 'CdtDbtInd' element on balance.", "Bal/CdtDbtInd"),
            Date = element.Path("Dt", "Dt").Date(),
            DateTime = element.Path("Dt", "DtTm").DateTime(),
            Source = element,
        };
    }

    private static TransactionsSummary? ReadTransactionsSummary(XElement? element, string? statementCurrency)
    {
        if (element is null)
            return null;

        var totalEntries = element.Child("TtlNtries");
        var totalCreditEntries = element.Child("TtlCdtNtries");
        var totalDebitEntries = element.Child("TtlDbtNtries");

        // Schema version .08 nests the net entry under 'TtlNetNtry/{Amt,CdtDbtInd}'; version .02 uses flat
        // sibling elements 'TtlNetNtryAmt' and 'CdtDbtInd' directly under 'TtlNtries'. Neither carries a
        // currency attribute, so the surrounding statement's account currency is used.
        var totalNetEntryAmount = totalEntries.Path("TtlNetNtry", "Amt").Decimal() ?? totalEntries.Child("TtlNetNtryAmt").Decimal();
        var totalNetCreditDebit = CommonReaders.ReadCreditDebit(totalEntries.Path("TtlNetNtry", "CdtDbtInd"))
            ?? CommonReaders.ReadCreditDebit(totalEntries.Child("CdtDbtInd"));

        return new TransactionsSummary
        {
            TotalEntries = totalEntries.Child("NbOfNtries").Int(),
            TotalSum = totalEntries.Child("Sum").Decimal(),
            TotalNetEntry = totalNetEntryAmount is null ? null : new Money(totalNetEntryAmount.Value, statementCurrency ?? string.Empty),
            TotalNetCreditDebit = totalNetCreditDebit,
            TotalCreditEntries = totalCreditEntries.Child("NbOfNtries").Int(),
            TotalCreditSum = totalCreditEntries.Child("Sum").Decimal(),
            TotalDebitEntries = totalDebitEntries.Child("NbOfNtries").Int(),
            TotalDebitSum = totalDebitEntries.Child("Sum").Decimal(),
            Source = element,
        };
    }

    private static StatementEntry ReadEntry(XElement element, string? statementCurrency)
    {
        var status = element.Child("Sts");

        return new StatementEntry
        {
            EntryReference = element.Child("NtryRef").Value(),
            Amount = ReadMoney(element.Child("Amt"), statementCurrency)
                ?? throw new Iso20022ValidationException("Missing 'Amt' element on entry.", "Ntry/Amt"),
            CreditDebit = CommonReaders.ReadCreditDebit(element.Child("CdtDbtInd"))
                ?? throw new Iso20022ValidationException("Missing 'CdtDbtInd' element on entry.", "Ntry/CdtDbtInd"),
            IsReversal = element.Child("RvslInd").Bool() ?? false,
            Status = status.Child("Cd").Value() ?? status.Child("Prtry").Value() ?? status.Value(),
            BookingDate = element.Path("BookgDt", "Dt").Date(),
            BookingDateTime = element.Path("BookgDt", "DtTm").DateTime(),
            ValueDate = element.Path("ValDt", "Dt").Date(),
            ValueDateTime = element.Path("ValDt", "DtTm").DateTime(),
            AccountServicerReference = element.Child("AcctSvcrRef").Value(),
            BankTransactionCode = CommonReaders.ReadBankTransactionCode(element.Child("BkTxCd")),
            Details = [.. element.Children("NtryDtls").Select(e => ReadEntryDetails(e, statementCurrency))],
            AdditionalEntryInformation = element.Child("AddtlNtryInf").Value(),
            Source = element,
        };
    }

    private static EntryDetails ReadEntryDetails(XElement element, string? statementCurrency)
    {
        var batch = element.Child("Btch");

        return new EntryDetails
        {
            BatchMessageId = batch.Child("MsgId").Value(),
            BatchPaymentInformationId = batch.Child("PmtInfId").Value(),
            BatchNumberOfTransactions = batch.Child("NbOfTxs").Int(),
            BatchTotalAmount = ReadMoney(batch.Child("TtlAmt"), statementCurrency),
            BatchCreditDebit = CommonReaders.ReadCreditDebit(batch.Child("CdtDbtInd")),
            Transactions = [.. element.Children("TxDtls").Select(e => ReadTransactionDetails(e, statementCurrency))],
            Source = element,
        };
    }

    private static TransactionDetails ReadTransactionDetails(XElement element, string? statementCurrency)
    {
        var relatedParties = element.Child("RltdPties");
        var relatedAgents = element.Child("RltdAgts");
        var purpose = element.Child("Purp");

        return new TransactionDetails
        {
            References = ReadReferences(element.Child("Refs")),
            Amount = ReadMoney(element.Child("Amt"), statementCurrency),
            CreditDebit = CommonReaders.ReadCreditDebit(element.Child("CdtDbtInd")),
            AmountDetails = ReadAmountDetails(element.Child("AmtDtls"), statementCurrency),
            BankTransactionCode = CommonReaders.ReadBankTransactionCode(element.Child("BkTxCd")),
            Debtor = CommonReaders.ReadParty(relatedParties.Child("Dbtr")),
            DebtorAccount = CommonReaders.ReadAccount(relatedParties.Child("DbtrAcct")),
            UltimateDebtor = CommonReaders.ReadParty(relatedParties.Child("UltmtDbtr")),
            Creditor = CommonReaders.ReadParty(relatedParties.Child("Cdtr")),
            CreditorAccount = CommonReaders.ReadAccount(relatedParties.Child("CdtrAcct")),
            UltimateCreditor = CommonReaders.ReadParty(relatedParties.Child("UltmtCdtr")),
            DebtorAgent = CommonReaders.ReadFinancialInstitution(relatedAgents.Child("DbtrAgt")),
            CreditorAgent = CommonReaders.ReadFinancialInstitution(relatedAgents.Child("CdtrAgt")),
            PurposeCode = purpose.Child("Cd").Value(),
            PurposeProprietary = purpose.Child("Prtry").Value(),
            RemittanceInformation = CommonReaders.ReadRemittance(element.Child("RmtInf")),
            AcceptanceDateTime = element.Path("RltdDts", "AccptncDtTm").DateTime(),
            ReturnInformation = ReadReturnInformation(element.Child("RtrInf")),
            AdditionalTransactionInformation = element.Child("AddtlTxInf").Value(),
            Source = element,
        };
    }

    private static TransactionReferences ReadReferences(XElement? element)
    {
        if (element is null)
            return new TransactionReferences();

        return new TransactionReferences
        {
            MessageId = element.Child("MsgId").Value(),
            AccountServicerReference = element.Child("AcctSvcrRef").Value(),
            PaymentInformationId = element.Child("PmtInfId").Value(),
            InstructionId = element.Child("InstrId").Value(),
            EndToEndId = element.Child("EndToEndId").Value(),
            TransactionId = element.Child("TxId").Value(),
            Uetr = element.Child("UETR").Value(),
            MandateId = element.Child("MndtId").Value(),
            ChequeNumber = element.Child("ChqNb").Value(),
            ClearingSystemReference = element.Child("ClrSysRef").Value(),
            Proprietary = [.. element.Children("Prtry").Select(ReadProprietaryReference)],
            Source = element,
        };
    }

    private static (string Type, string Reference) ReadProprietaryReference(XElement element) =>
        (element.Child("Tp").Value() ?? string.Empty, element.Child("Ref").Value() ?? string.Empty);

    private static AmountDetails? ReadAmountDetails(XElement? element, string? statementCurrency)
    {
        if (element is null)
            return null;

        return new AmountDetails
        {
            InstructedAmount = ReadMoney(element.Path("InstdAmt", "Amt"), statementCurrency),
            TransactionAmount = ReadMoney(element.Path("TxAmt", "Amt"), statementCurrency),
            CounterValueAmount = ReadMoney(element.Path("CntrValAmt", "Amt"), statementCurrency),
            ExchangeRate = element.Path("CntrValAmt", "CcyXchg", "XchgRate").Decimal(),
            Source = element,
        };
    }

    private static ReturnInformation? ReadReturnInformation(XElement? element)
    {
        if (element is null)
            return null;

        return new ReturnInformation
        {
            OriginalBankTransactionCode = CommonReaders.ReadBankTransactionCode(element.Child("OrgnlBkTxCd")),
            Originator = CommonReaders.ReadParty(element.Child("Orgtr")),
            ReasonCode = element.Path("Rsn", "Cd").Value(),
            ReasonProprietary = element.Path("Rsn", "Prtry").Value(),
            AdditionalInformation = [.. element.Children("AddtlInf").Select(e => e.Value()).OfType<string>()],
            Source = element,
        };
    }

    /// <summary>Reads a money amount, falling back to <paramref name="fallbackCurrency"/> when the amount element has no <c>Ccy</c> attribute.</summary>
    private static Money? ReadMoney(XElement? element, string? fallbackCurrency)
    {
        var money = element.Money();
        if (money is null)
            return null;

        return money.Value.Currency.Length != 0 || fallbackCurrency is null
            ? money
            : money.Value with { Currency = fallbackCurrency };
    }
}
