using System.Text;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Tests.Iso20022.Common;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Camt;

public class CamtReaderTests
{
    public static TheoryData<string, bool> Camt053Samples => new()
    {
        { "camt.053.001.02-sample.xml", false },
        { "camt.053.001.08-sample.xml", true },
    };

    public static TheoryData<string> Camt053SampleFileNames => new()
    {
        "camt.053.001.02-sample.xml",
        "camt.053.001.08-sample.xml",
    };

    [Theory]
    [MemberData(nameof(Camt053Samples))]
    public void Read_Camt053Sample_ReadsGroupHeader(string fileName, bool isV08)
    {
        var message = CamtReader.Read(SampleXml.Load(fileName));

        message.Identifier.Type.ShouldBe(Iso20022MessageType.Camt053);
        message.Identifier.Identifier.ShouldBe(isV08 ? "camt.053.001.08" : "camt.053.001.02");
        message.GroupHeader.MessageId.ShouldBe("MSGID-20260901-0001");
        message.GroupHeader.CreationDateTime.ShouldBe(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.FromHours(2)));
        message.GroupHeader.MessageRecipientName.ShouldBe("Example Debtor GmbH");
        message.GroupHeader.PageNumber.ShouldBe(1);
        message.GroupHeader.LastPageIndicator.ShouldBe(true);
        message.GroupHeader.AdditionalInformation.ShouldBe("Monthly account statement");
        message.GroupHeader.Source.ShouldNotBeNull();
        message.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Camt053SampleFileNames))]
    public void Read_Camt053Sample_ReadsFirstStatementHeaderAndAccount(string fileName)
    {
        var message = CamtReader.Read(SampleXml.Load(fileName));
        message.Statements.Count.ShouldBe(2);
        var statement = message.Statements[0];

        statement.Kind.ShouldBe(StatementKind.Statement);
        statement.Id.ShouldBe("STMT-0001");
        statement.ElectronicSequenceNumber.ShouldBe(12L);
        statement.LegalSequenceNumber.ShouldBe(5L);
        statement.CreationDateTime.ShouldBe(new DateTimeOffset(2026, 9, 1, 8, 5, 0, TimeSpan.Zero));
        statement.FromDateTime.ShouldBe(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(2)));
        statement.ToDateTime.ShouldBe(new DateTimeOffset(2026, 8, 31, 23, 59, 59, TimeSpan.FromHours(2)));
        statement.CopyDuplicateIndicator.ShouldBeNull();

        statement.Account.ShouldNotBeNull();
        statement.Account!.Iban.ShouldBe("DE89370400440532013000");
        statement.Account.Currency.ShouldBe("EUR");
        statement.Account.Name.ShouldBe("Example Debtor GmbH Main Account");
        statement.AccountOwnerName.ShouldBe("Example Debtor GmbH");
        statement.AccountServicer.ShouldNotBeNull();
        statement.AccountServicer!.Bic.ShouldBe("COBADEFFXXX");
        statement.AccountServicer.Name.ShouldBe("Commerzbank AG");
        statement.AdditionalStatementInformation.ShouldBe("Statement covers August 2026");
        statement.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Camt053SampleFileNames))]
    public void Read_Camt053Sample_ReadsBalancesAndSummary(string fileName)
    {
        var statement = CamtReader.Read(SampleXml.Load(fileName)).Statements[0];

        statement.Balances.Count.ShouldBe(2);
        var opening = statement.Balances[0];
        opening.TypeCode.ShouldBe("OPBD");
        opening.Amount.ShouldBe(new Money(10000.00m, "EUR"));
        opening.CreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        opening.Date.ShouldBe(new DateOnly(2026, 8, 1));
        opening.Source.ShouldNotBeNull();

        var closing = statement.Balances[1];
        closing.TypeCode.ShouldBe("CLBD");
        closing.Amount.ShouldBe(new Money(10500.00m, "EUR"));
        closing.CreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        closing.Date.ShouldBe(new DateOnly(2026, 8, 31));

        var summary = statement.TransactionsSummary;
        summary.ShouldNotBeNull();
        summary!.TotalEntries.ShouldBe(2);
        summary.TotalSum.ShouldBe(1500.00m);
        summary.TotalNetEntry.ShouldBe(new Money(500.00m, "EUR"));
        summary.TotalNetCreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        summary.TotalCreditEntries.ShouldBe(1);
        summary.TotalCreditSum.ShouldBe(1000.00m);
        summary.TotalDebitEntries.ShouldBe(1);
        summary.TotalDebitSum.ShouldBe(500.00m);
        summary.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Camt053Samples))]
    public void Read_Camt053Sample_ReadsBatchEntryWithTwoTransactions(string fileName, bool isV08)
    {
        var statement = CamtReader.Read(SampleXml.Load(fileName)).Statements[0];
        statement.Entries.Count.ShouldBe(2);

        var entry = statement.Entries[0];
        entry.EntryReference.ShouldBe("ENTRYREF-0001");
        entry.Amount.ShouldBe(new Money(1000.00m, "EUR"));
        entry.CreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        entry.IsReversal.ShouldBeFalse();
        entry.Status.ShouldBe("BOOK");
        entry.BookingDate.ShouldBe(new DateOnly(2026, 8, 15));
        entry.ValueDate.ShouldBe(new DateOnly(2026, 8, 15));
        entry.AccountServicerReference.ShouldBe("ASR-0001");
        entry.BankTransactionCode.ShouldNotBeNull();
        entry.BankTransactionCode!.Domain.ShouldBe("PMNT");
        entry.BankTransactionCode.Family.ShouldBe("RCDT");
        entry.BankTransactionCode.SubFamily.ShouldBe("ESCT");
        entry.AdditionalEntryInformation.ShouldBe("Batch collection of two invoices");
        entry.Source.ShouldNotBeNull();

        entry.Details.Count.ShouldBe(1);
        var details = entry.Details[0];
        details.BatchMessageId.ShouldBe("BATCHMSG-0001");
        details.BatchPaymentInformationId.ShouldBe("BATCHPMT-0001");
        details.BatchNumberOfTransactions.ShouldBe(2);
        details.BatchTotalAmount.ShouldBe(new Money(1000.00m, "EUR"));
        details.BatchCreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        details.Source.ShouldNotBeNull();

        details.Transactions.Count.ShouldBe(2);
        var first = details.Transactions[0];
        first.References.EndToEndId.ShouldBe("E2E-0001");
        first.References.InstructionId.ShouldBe("INSTR-0001");
        first.References.TransactionId.ShouldBe("TXID-0001");
        first.References.Uetr.ShouldBe(isV08 ? "8a562c67-ca16-48ba-b074-65581be6f001" : null);
        first.References.Source.ShouldNotBeNull();
        first.AmountDetails.ShouldNotBeNull();
        first.AmountDetails!.InstructedAmount.ShouldBe(new Money(600.00m, "EUR"));
        first.Debtor.ShouldNotBeNull();
        first.Debtor!.Name.ShouldBe("Example Creditor Ltd");
        first.DebtorAccount.ShouldNotBeNull();
        first.DebtorAccount!.Iban.ShouldBe("DE02120300000000202051");
        first.Creditor.ShouldNotBeNull();
        first.Creditor!.Name.ShouldBe("Example Debtor GmbH");
        first.CreditorAccount.ShouldNotBeNull();
        first.CreditorAccount!.Iban.ShouldBe("DE89370400440532013000");
        first.RemittanceInformation.ShouldNotBeNull();
        first.RemittanceInformation!.Unstructured.ShouldBe(["Invoice 2026-0917"]);
        first.Source.ShouldNotBeNull();

        if (isV08)
        {
            first.Amount.ShouldBe(new Money(600.00m, "EUR"));
            first.CreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        }
        else
        {
            first.Amount.ShouldBeNull();
            first.CreditDebit.ShouldBeNull();
        }

        var second = details.Transactions[1];
        second.References.EndToEndId.ShouldBe("E2E-0002");
        second.AmountDetails.ShouldNotBeNull();
        second.AmountDetails!.InstructedAmount.ShouldBe(new Money(400.00m, "EUR"));
        second.RemittanceInformation.ShouldNotBeNull();
        second.RemittanceInformation!.Unstructured.ShouldBe(["Invoice 2026-0918"]);
    }

    [Theory]
    [MemberData(nameof(Camt053SampleFileNames))]
    public void Read_Camt053Sample_ReadsSepaCreditTransferEntryWithStructuredRemittance(string fileName)
    {
        var statement = CamtReader.Read(SampleXml.Load(fileName)).Statements[0];
        var entry = statement.Entries[1];

        entry.EntryReference.ShouldBe("ENTRYREF-0002");
        entry.Amount.ShouldBe(new Money(500.00m, "EUR"));
        entry.CreditDebit.ShouldBe(CreditDebitIndicator.Debit);
        entry.BankTransactionCode!.Family.ShouldBe("ICDT");
        entry.Details.Count.ShouldBe(1);
        entry.Details[0].BatchMessageId.ShouldBeNull();
        entry.Details[0].Transactions.Count.ShouldBe(1);

        var transaction = entry.Details[0].Transactions[0];
        transaction.References.EndToEndId.ShouldBe("E2E-0003");
        transaction.Debtor.ShouldNotBeNull();
        transaction.Debtor!.Name.ShouldBe("Example Debtor GmbH");
        transaction.Debtor.PostalAddress.ShouldNotBeNull();
        transaction.Debtor.PostalAddress!.Country.ShouldBe("DE");
        transaction.Debtor.PostalAddress.TownName.ShouldBe("Frankfurt am Main");
        transaction.Debtor.PostalAddress.PostCode.ShouldBe("60311");
        transaction.Debtor.OrganisationId.ShouldNotBeNull();
        transaction.Debtor.OrganisationId!.Bic.ShouldBe("COBADEFFXXX");
        transaction.Creditor.ShouldNotBeNull();
        transaction.Creditor!.Name.ShouldBe("Example Creditor Ltd");
        transaction.DebtorAgent.ShouldNotBeNull();
        transaction.DebtorAgent!.Bic.ShouldBe("COBADEFFXXX");
        transaction.DebtorAgent.Name.ShouldBe("Commerzbank AG");
        transaction.CreditorAgent.ShouldNotBeNull();
        transaction.CreditorAgent!.Bic.ShouldBe("MARKDEF1100");
        transaction.CreditorAgent.Name.ShouldBe("Deutsche Bundesbank");
        transaction.PurposeCode.ShouldBe("SUPP");
        transaction.RemittanceInformation.ShouldNotBeNull();
        transaction.RemittanceInformation!.CreditorReferenceTypeCode.ShouldBe("SCOR");
        transaction.RemittanceInformation.CreditorReferenceIssuer.ShouldBe("Example Creditor Ltd");
        transaction.RemittanceInformation.CreditorReference.ShouldBe("RF18539007547034");
    }

    [Theory]
    [MemberData(nameof(Camt053SampleFileNames))]
    public void Read_Camt053Sample_ReadsSecondStatementWithNonIbanAccountAndReturnedEntry(string fileName)
    {
        var statement = CamtReader.Read(SampleXml.Load(fileName)).Statements[1];

        statement.Id.ShouldBe("STMT-0002");
        statement.ElectronicSequenceNumber.ShouldBe(13L);
        statement.LegalSequenceNumber.ShouldBeNull();
        statement.CopyDuplicateIndicator.ShouldBe("CODU");
        statement.Account.ShouldNotBeNull();
        statement.Account!.Iban.ShouldBeNull();
        statement.Account.OtherId.ShouldBe("ACCT-0002");
        statement.Account.OtherSchemeCode.ShouldBe("BBAN");
        statement.AdditionalStatementInformation.ShouldBe("Statement includes one returned direct debit");

        statement.TransactionsSummary.ShouldNotBeNull();
        statement.TransactionsSummary!.TotalNetCreditDebit.ShouldBe(CreditDebitIndicator.Debit);
        statement.TransactionsSummary.TotalCreditEntries.ShouldBe(0);

        statement.Entries.Count.ShouldBe(1);
        var entry = statement.Entries[0];
        entry.EntryReference.ShouldBe("ENTRYREF-0003");
        entry.CreditDebit.ShouldBe(CreditDebitIndicator.Debit);
        entry.IsReversal.ShouldBeTrue();
        entry.BankTransactionCode!.Family.ShouldBe("RDDT");

        var transaction = entry.Details[0].Transactions[0];
        transaction.References.MandateId.ShouldBe("MANDATE-0001");
        transaction.ReturnInformation.ShouldNotBeNull();
        transaction.ReturnInformation!.OriginalBankTransactionCode.ShouldNotBeNull();
        transaction.ReturnInformation.OriginalBankTransactionCode!.Family.ShouldBe("ICDT");
        transaction.ReturnInformation.Originator.ShouldNotBeNull();
        transaction.ReturnInformation.Originator!.Name.ShouldBe("Example Creditor Ltd");
        transaction.ReturnInformation.ReasonCode.ShouldBe("AC04");
        transaction.ReturnInformation.AdditionalInformation.ShouldBe(["Account closed"]);
        transaction.ReturnInformation.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_Camt052Report_ReadsReportKindAndItbdBalance()
    {
        var message = CamtReader.Read(SampleXml.Load("camt.052.001.08-sample.xml"));

        message.Identifier.Type.ShouldBe(Iso20022MessageType.Camt052);
        message.Statements.Count.ShouldBe(1);

        var report = message.Statements[0];
        report.Kind.ShouldBe(StatementKind.Report);
        report.Id.ShouldBe("RPT-0001");
        report.Account.ShouldNotBeNull();
        report.Account!.Iban.ShouldBe("DE89370400440532013000");

        report.Balances.Count.ShouldBe(1);
        var balance = report.Balances[0];
        balance.TypeCode.ShouldBe("ITBD");
        balance.Amount.ShouldBe(new Money(10650.00m, "EUR"));
        balance.CreditDebit.ShouldBe(CreditDebitIndicator.Credit);
        balance.DateTime.ShouldBe(new DateTimeOffset(2026, 9, 1, 15, 0, 0, TimeSpan.FromHours(2)));

        report.Entries.Count.ShouldBe(1);
        report.Entries[0].Amount.ShouldBe(new Money(150.00m, "EUR"));
    }

    [Fact]
    public void Read_Camt054Notification_ReadsEntryWithoutBalances()
    {
        var message = CamtReader.Read(SampleXml.Load("camt.054.001.08-sample.xml"));

        message.Identifier.Type.ShouldBe(Iso20022MessageType.Camt054);
        message.Statements.Count.ShouldBe(1);

        var notification = message.Statements[0];
        notification.Kind.ShouldBe(StatementKind.Notification);
        notification.Id.ShouldBe("NTFCTN-0001");
        notification.Balances.ShouldBeEmpty();
        notification.TransactionsSummary.ShouldBeNull();

        notification.Entries.Count.ShouldBe(1);
        var entry = notification.Entries[0];
        entry.Amount.ShouldBe(new Money(275.50m, "EUR"));
        entry.Details[0].Transactions[0].RemittanceInformation.ShouldNotBeNull();
        entry.Details[0].Transactions[0].RemittanceInformation!.Unstructured.ShouldBe(["Advance notice of incoming payment"]);
    }

    [Fact]
    public void Read_RootWithoutRecognizedChild_ThrowsValidationException()
    {
        const string xml = "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:camt.053.001.08\"><SomethingElse/></Document>";

        var exception = Should.Throw<Iso20022ValidationException>(() => CamtReader.Read(xml));
        exception.Message.ShouldContain("BkToCstmrAcctRpt");
        exception.Message.ShouldContain("SomethingElse");
        exception.Path.ShouldBe("Document/BkToCstmrStmt");
    }

    [Fact]
    public void Read_RootNotNamedDocument_ThrowsValidationExceptionWithDocumentPath()
    {
        const string xml = "<Foo xmlns=\"urn:iso:std:iso:20022:tech:xsd:camt.053.001.08\"><BkToCstmrStmt><GrpHdr><MsgId>MSGID-1</MsgId></GrpHdr></BkToCstmrStmt></Foo>";

        var exception = Should.Throw<Iso20022ValidationException>(() => CamtReader.Read(xml));
        exception.Path.ShouldBe("Document");
    }

    [Fact]
    public void Read_BalanceWithoutAmt_ThrowsValidationExceptionWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
              <BkToCstmrStmt>
                <GrpHdr><MsgId>MSGID-1</MsgId></GrpHdr>
                <Stmt>
                  <Id>STMT-1</Id>
                  <Bal>
                    <Tp><CdOrPrtry><Cd>OPBD</Cd></CdOrPrtry></Tp>
                    <CdtDbtInd>CRDT</CdtDbtInd>
                  </Bal>
                </Stmt>
              </BkToCstmrStmt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => CamtReader.Read(xml));
        exception.Path.ShouldBe("Bal/Amt");
    }

    [Fact]
    public void Read_EntryWithoutAmt_ThrowsValidationExceptionWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
              <BkToCstmrStmt>
                <GrpHdr><MsgId>MSGID-1</MsgId></GrpHdr>
                <Stmt>
                  <Id>STMT-1</Id>
                  <Ntry>
                    <CdtDbtInd>CRDT</CdtDbtInd>
                  </Ntry>
                </Stmt>
              </BkToCstmrStmt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => CamtReader.Read(xml));
        exception.Path.ShouldBe("Ntry/Amt");
    }

    [Fact]
    public void Read_StreamAndString_ProduceEquivalentResults()
    {
        var xml = SampleXml.Load("camt.053.001.08-sample.xml");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var fromString = CamtReader.Read(xml);
        var fromStream = CamtReader.Read(stream);

        fromStream.GroupHeader.MessageId.ShouldBe(fromString.GroupHeader.MessageId);
        fromStream.Statements.Count.ShouldBe(fromString.Statements.Count);
        fromStream.Statements[0].Entries.Count.ShouldBe(fromString.Statements[0].Entries.Count);
        fromStream.Statements[0].Entries[0].Amount.ShouldBe(fromString.Statements[0].Entries[0].Amount);
    }
}
