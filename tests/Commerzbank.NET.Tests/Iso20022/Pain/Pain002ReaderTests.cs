using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Tests.Iso20022.Common;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Pain;

public class Pain002ReaderTests
{
    public static TheoryData<string, bool> Samples => new()
    {
        { "pain.002.001.03-sample.xml", false },
        { "pain.002.001.10-sample.xml", true },
    };

    public static TheoryData<string> SampleFileNames => new()
    {
        "pain.002.001.03-sample.xml",
        "pain.002.001.10-sample.xml",
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void Read_Sample_ReadsGroupHeader(string fileName, bool isV10)
    {
        var report = Pain002Reader.Read(SampleXml.Load(fileName));

        report.Identifier.Type.ShouldBe(Iso20022MessageType.Pain002);
        report.Identifier.Identifier.ShouldBe(isV10 ? "pain.002.001.10" : "pain.002.001.03");
        report.MessageId.ShouldBe("STSMSG-20260901-0001");
        report.CreationDateTime.ShouldBe(new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.FromHours(2)));
        report.InitiatingParty.ShouldNotBeNull();
        report.InitiatingParty!.Name.ShouldBe("Commerzbank AG");
        report.DebtorAgent.ShouldNotBeNull();
        report.DebtorAgent!.Bic.ShouldBe("COBADEFFXXX");
        report.CreditorAgent.ShouldNotBeNull();
        report.CreditorAgent!.Bic.ShouldBe("MARKDEF1100");
        report.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Read_Sample_ReadsOriginalGroupStatus(string fileName, bool isV10)
    {
        var report = Pain002Reader.Read(SampleXml.Load(fileName));
        var group = report.OriginalGroup;

        group.OriginalMessageId.ShouldBe("MSGID-20260831-0007");
        group.OriginalMessageNameId.ShouldBe(isV10 ? "pain.001.001.09" : "pain.001.001.03");
        group.OriginalCreationDateTime.ShouldBe(new DateTimeOffset(2026, 8, 31, 15, 30, 0, TimeSpan.FromHours(2)));
        group.OriginalNumberOfTransactions.ShouldBe(2);
        group.OriginalControlSum.ShouldBe(1500.00m);
        group.GroupStatus.ShouldBe("RJCT");

        group.StatusReasons.Count.ShouldBe(1);
        group.StatusReasons[0].Code.ShouldBe("AM04");
        group.StatusReasons[0].AdditionalInformation.ShouldContain("Insufficient funds for one transaction in the batch");

        group.NumberOfTransactionsPerStatus.Count.ShouldBe(2);
        group.NumberOfTransactionsPerStatus[0].ShouldBe(new TransactionCountPerStatus("ACSC", 1, 1000.00m));
        group.NumberOfTransactionsPerStatus[1].ShouldBe(new TransactionCountPerStatus("RJCT", 1, 500.00m));
        group.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(SampleFileNames))]
    public void Read_Sample_ReadsPaymentInformationStatus(string fileName)
    {
        var report = Pain002Reader.Read(SampleXml.Load(fileName));

        report.PaymentInformations.Count.ShouldBe(1);
        var paymentInformation = report.PaymentInformations[0];

        paymentInformation.OriginalPaymentInformationId.ShouldBe("PMTINF-20260831-0001");
        paymentInformation.OriginalNumberOfTransactions.ShouldBe(2);
        paymentInformation.OriginalControlSum.ShouldBe(1500.00m);
        paymentInformation.PaymentInformationStatus.ShouldBe("PART");
        paymentInformation.Source.ShouldNotBeNull();
        paymentInformation.Transactions.Count.ShouldBe(2);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Read_Sample_ReadsAcceptedTransactionStatus(string fileName, bool isV10)
    {
        var transaction = Pain002Reader.Read(SampleXml.Load(fileName)).PaymentInformations[0].Transactions[0];

        transaction.StatusId.ShouldBe("STS-0001");
        transaction.OriginalInstructionId.ShouldBe("INSTR-0001");
        transaction.OriginalEndToEndId.ShouldBe("E2E-0001");
        transaction.OriginalUetr.ShouldBe(isV10 ? "8a562c67-ca16-48ba-b074-65581be6f101" : null);
        transaction.Status.ShouldBe("ACSC");
        transaction.StatusReasons.ShouldBeEmpty();
        transaction.AcceptanceDateTime.ShouldBe(new DateTimeOffset(2026, 8, 31, 15, 31, 0, TimeSpan.FromHours(2)));
        transaction.AccountServicerReference.ShouldBe("ASR-0001");
        transaction.OriginalTransaction.ShouldBeNull();
        transaction.Source.ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Read_Sample_ReadsRejectedTransactionStatusWithReasonAndOriginalTransactionReference(string fileName, bool isV10)
    {
        var transaction = Pain002Reader.Read(SampleXml.Load(fileName)).PaymentInformations[0].Transactions[1];

        transaction.StatusId.ShouldBe("STS-0002");
        transaction.OriginalInstructionId.ShouldBe("INSTR-0002");
        transaction.OriginalEndToEndId.ShouldBe("E2E-0002");
        transaction.OriginalUetr.ShouldBe(isV10 ? "8a562c67-ca16-48ba-b074-65581be6f102" : null);
        transaction.Status.ShouldBe("RJCT");
        transaction.AccountServicerReference.ShouldBe("ASR-0002");

        transaction.StatusReasons.Count.ShouldBe(1);
        var reason = transaction.StatusReasons[0];
        reason.Code.ShouldBe("AC01");
        reason.Originator.ShouldNotBeNull();
        reason.Originator!.Name.ShouldBe("Commerzbank AG");
        reason.AdditionalInformation.ShouldContain("Incorrect account number");

        var original = transaction.OriginalTransaction;
        original.ShouldNotBeNull();
        original!.Amount.ShouldBe(new Money(500.00m, "EUR"));
        original.RequestedExecutionDate.ShouldBe(new DateOnly(2026, 8, 31));
        original.PaymentMethod.ShouldBe("TRF");
        original.PaymentTypeInformation.ShouldNotBeNull();
        original.PaymentTypeInformation!.ServiceLevelCode.ShouldBe("SEPA");
        original.RemittanceInformation.ShouldNotBeNull();
        original.RemittanceInformation!.Unstructured.ShouldBe(["Invoice 2026-0920"]);
        original.Debtor.ShouldNotBeNull();
        original.Debtor!.Name.ShouldBe("Example Debtor GmbH");
        original.DebtorAccount.ShouldNotBeNull();
        original.DebtorAccount!.Iban.ShouldBe("DE89370400440532013000");
        original.DebtorAgent.ShouldNotBeNull();
        original.DebtorAgent!.Bic.ShouldBe("COBADEFFXXX");
        original.CreditorAgent.ShouldNotBeNull();
        original.CreditorAgent!.Bic.ShouldBe("MARKDEF1100");
        original.Creditor.ShouldNotBeNull();
        original.Creditor!.Name.ShouldBe("Example Creditor Ltd");
        original.CreditorAccount.ShouldNotBeNull();
        original.CreditorAccount!.Iban.ShouldBe("DE02120300000000202051");
        original.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_RootElementNotDocument_ThrowsWithPath()
    {
        const string xml = "<NotDocument xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.002.001.03\"/>";

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("Document");
    }

    [Fact]
    public void Read_MissingCstmrPmtStsRptChild_ThrowsWithPath()
    {
        const string xml = "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pain.002.001.03\"><SomethingElse/></Document>";

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("Document/CstmrPmtStsRpt");
    }

    [Fact]
    public void Read_MissingOriginalGroupInformation_ThrowsWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.002.001.03">
              <CstmrPmtStsRpt>
                <GrpHdr>
                  <MsgId>MSGID-0001</MsgId>
                  <CreDtTm>2026-09-01T09:00:00</CreDtTm>
                </GrpHdr>
              </CstmrPmtStsRpt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("OrgnlGrpInfAndSts");
    }

    [Fact]
    public void Read_TransactionCountPerStatusMissingDetailedStatus_ThrowsWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.002.001.03">
              <CstmrPmtStsRpt>
                <GrpHdr>
                  <MsgId>MSGID-0001</MsgId>
                  <CreDtTm>2026-09-01T09:00:00</CreDtTm>
                </GrpHdr>
                <OrgnlGrpInfAndSts>
                  <OrgnlMsgId>MSGID-ORIG</OrgnlMsgId>
                  <OrgnlMsgNmId>pain.001.001.03</OrgnlMsgNmId>
                  <GrpSts>RJCT</GrpSts>
                  <NbOfTxsPerSts>
                    <DtldNbOfTxs>1</DtldNbOfTxs>
                  </NbOfTxsPerSts>
                </OrgnlGrpInfAndSts>
              </CstmrPmtStsRpt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("NbOfTxsPerSts/DtldSts");
    }

    [Fact]
    public void Read_TransactionCountPerStatusMissingDetailedCount_ThrowsWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.002.001.03">
              <CstmrPmtStsRpt>
                <GrpHdr>
                  <MsgId>MSGID-0001</MsgId>
                  <CreDtTm>2026-09-01T09:00:00</CreDtTm>
                </GrpHdr>
                <OrgnlGrpInfAndSts>
                  <OrgnlMsgId>MSGID-ORIG</OrgnlMsgId>
                  <OrgnlMsgNmId>pain.001.001.03</OrgnlMsgNmId>
                  <GrpSts>RJCT</GrpSts>
                  <NbOfTxsPerSts>
                    <DtldSts>ACSC</DtldSts>
                  </NbOfTxsPerSts>
                </OrgnlGrpInfAndSts>
              </CstmrPmtStsRpt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("NbOfTxsPerSts/DtldNbOfTxs");
    }

    [Fact]
    public void Read_TransactionCountPerStatusUnparsableDetailedCount_ThrowsValidationExceptionWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.002.001.03">
              <CstmrPmtStsRpt>
                <GrpHdr>
                  <MsgId>MSGID-0001</MsgId>
                  <CreDtTm>2026-09-01T09:00:00</CreDtTm>
                </GrpHdr>
                <OrgnlGrpInfAndSts>
                  <OrgnlMsgId>MSGID-ORIG</OrgnlMsgId>
                  <OrgnlMsgNmId>pain.001.001.03</OrgnlMsgNmId>
                  <GrpSts>RJCT</GrpSts>
                  <NbOfTxsPerSts>
                    <DtldSts>ACSC</DtldSts>
                    <DtldNbOfTxs>not-a-number</DtldNbOfTxs>
                  </NbOfTxsPerSts>
                </OrgnlGrpInfAndSts>
              </CstmrPmtStsRpt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain002Reader.Read(xml));

        exception.Path.ShouldBe("NbOfTxsPerSts/DtldNbOfTxs");
    }
}
