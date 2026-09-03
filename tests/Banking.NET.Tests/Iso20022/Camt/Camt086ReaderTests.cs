using System.Text;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Banking.NET.Tests.Iso20022.Common;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Iso20022.Camt;

public class Camt086ReaderTests
{
    [Fact]
    public void Read_Sample_ReadsHeader()
    {
        var message = CamtReaderTestHelpers.ReadCamt086Sample();

        message.Identifier.Type.ShouldBe(Iso20022MessageType.Camt086);
        message.Identifier.Identifier.ShouldBe("camt.086.001.02");
        message.ReportId.ShouldBe("BILLRPT-0001");
        message.PageNumber.ShouldBe(1);
        message.LastPageIndicator.ShouldBe(true);
        message.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_Sample_ReadsGroupAndSenderReceiver()
    {
        var message = CamtReaderTestHelpers.ReadCamt086Sample();

        message.Groups.Count.ShouldBe(1);
        var group = message.Groups[0];
        group.GroupId.ShouldBe("BILLGRP-0001");
        group.Sender.ShouldNotBeNull();
        group.Sender!.Name.ShouldBe("Commerzbank AG");
        group.Receiver.ShouldNotBeNull();
        group.Receiver!.Name.ShouldBe("Example Debtor GmbH");
        group.Statements.Count.ShouldBe(1);
        group.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_Sample_ReadsStatementHeaderAndAccount()
    {
        var statement = CamtReaderTestHelpers.ReadCamt086Sample().Groups[0].Statements[0];

        statement.StatementId.ShouldBe("BILLSTMT-0001");
        statement.FromDate.ShouldBe(new DateOnly(2026, 8, 1));
        statement.ToDate.ShouldBe(new DateOnly(2026, 8, 31));
        statement.CreationDateTime.ShouldBe(new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.FromHours(2)));
        statement.Status.ShouldBe("ORGN");
        statement.AccountLevel.ShouldBe("SMRY");
        statement.Account.ShouldNotBeNull();
        statement.Account!.Iban.ShouldBe("DE89370400440532013000");
        statement.AccountServicer.ShouldNotBeNull();
        statement.AccountServicer!.Bic.ShouldBe("COBADEFFXXX");
        statement.AccountServicer.Name.ShouldBe("Commerzbank AG");
        statement.CompensationMethod.ShouldBe("DDBT");
        statement.AccountBalanceCurrency.ShouldBe("EUR");
        statement.SettlementCurrency.ShouldBe("EUR");
        statement.HostCurrency.ShouldBe("EUR");
        statement.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_Sample_ReadsBalance()
    {
        var statement = CamtReaderTestHelpers.ReadCamt086Sample().Groups[0].Statements[0];

        statement.Balances.Count.ShouldBe(1);
        var balance = statement.Balances[0];
        balance.TypeCode.ShouldBe("CLBD");
        balance.Amount.ShouldBe(new Money(45.90m, "EUR"));
        balance.CreditDebit.ShouldBe(CreditDebitIndicator.Debit);
        balance.Source.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("true", CreditDebitIndicator.Debit)]
    [InlineData("false", CreditDebitIndicator.Credit)]
    [InlineData(null, CreditDebitIndicator.Credit)]
    public void Read_BalanceValSgn_DeterminesCreditDebit(string? sign, CreditDebitIndicator expected)
    {
        var signElement = sign is null ? string.Empty : $"<Sgn>{sign}</Sgn>";
        var xml = $"""
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.086.001.02">
              <BkSvcsBllgStmt>
                <BllgStmtGrp>
                  <BllgStmt>
                    <StmtId>BILLSTMT-1</StmtId>
                    <Bal>
                      <Tp><Cd>CLBD</Cd></Tp>
                      <Val>
                        <Amt Ccy="EUR">10.00</Amt>
                        {signElement}
                      </Val>
                    </Bal>
                  </BllgStmt>
                </BllgStmtGrp>
              </BkSvcsBllgStmt>
            </Document>
            """;

        var balance = Camt086Reader.Read(xml).Groups[0].Statements[0].Balances[0];

        balance.Amount.ShouldBe(new Money(10.00m, "EUR"));
        balance.CreditDebit.ShouldBe(expected);
    }

    [Fact]
    public void Read_Sample_ReadsThreeServices()
    {
        var statement = CamtReaderTestHelpers.ReadCamt086Sample().Groups[0].Statements[0];
        statement.Services.Count.ShouldBe(3);

        var accountMaintenance = statement.Services[0];
        accountMaintenance.ServiceId.ShouldBe("ACCTMAINT");
        accountMaintenance.SubServiceCode.ShouldBe("MONTHLY");
        accountMaintenance.SubServiceIssuer.ShouldBe("MACT");
        accountMaintenance.Description.ShouldBe("Account maintenance fee");
        accountMaintenance.CommonCode.ShouldBe("A001");
        accountMaintenance.CommonCodeIssuer.ShouldBe("CMZ");
        accountMaintenance.ServiceType.ShouldBe("STAN");
        accountMaintenance.BankTransactionCode.ShouldNotBeNull();
        accountMaintenance.BankTransactionCode!.Domain.ShouldBe("ACMT");
        accountMaintenance.BankTransactionCode.Family.ShouldBe("SVCC");
        accountMaintenance.BankTransactionCode.SubFamily.ShouldBe("FEES");
        accountMaintenance.Volume.ShouldBe(1m);
        accountMaintenance.PriceCurrency.ShouldBe("EUR");
        accountMaintenance.UnitPrice.ShouldBe(new Money(15.00m, "EUR"));
        accountMaintenance.PriceMethod.ShouldBe("FCHG");
        accountMaintenance.PaymentMethod.ShouldBe("FLAT");
        accountMaintenance.OriginalChargePrice.ShouldBe(new Money(15.00m, "EUR"));
        accountMaintenance.OriginalChargeSettlementAmount.ShouldBe(new Money(15.00m, "EUR"));
        accountMaintenance.TaxDesignation.ShouldBe("TAXE");
        accountMaintenance.Source.ShouldNotBeNull();

        var wireTransfer = statement.Services[1];
        wireTransfer.ServiceId.ShouldBe("WIRETRANSFER");
        wireTransfer.SubServiceIssuer.ShouldBe("SEQN");
        wireTransfer.Description.ShouldBe("Outgoing wire transfer fee");
        wireTransfer.CommonCode.ShouldBe("B002");
        wireTransfer.CommonCodeIssuer.ShouldBe("CMZ");
        wireTransfer.Volume.ShouldBe(5m);
        wireTransfer.UnitPrice.ShouldBe(new Money(5.00m, "EUR"));
        wireTransfer.PriceMethod.ShouldBe("UPRC");
        wireTransfer.PaymentMethod.ShouldBe("BCMP");
        wireTransfer.OriginalChargePrice.ShouldBe(new Money(25.00m, "EUR"));
        wireTransfer.TaxDesignation.ShouldBe("ZERO");

        var statementPrint = statement.Services[2];
        statementPrint.ServiceId.ShouldBe("STMTPRINT");
        statementPrint.Description.ShouldBe("Paper statement fee");
        statementPrint.CommonCode.ShouldBe("C003");
        statementPrint.CommonCodeIssuer.ShouldBe("CMZ");
        statementPrint.PriceMethod.ShouldBe("STAM");
        statementPrint.PaymentMethod.ShouldBe("INVS");
        statementPrint.TaxDesignation.ShouldBe("XMPT");
        statementPrint.OriginalChargePrice.ShouldBe(new Money(5.90m, "EUR"));
    }

    [Fact]
    public void Read_ServiceAmountsWithSgnTrue_NegatesAmounts()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.086.001.02">
              <BkSvcsBllgStmt>
                <BllgStmtGrp>
                  <BllgStmt>
                    <StmtId>BILLSTMT-1</StmtId>
                    <Svc>
                      <SvcDtl>
                        <BkSvc>
                          <Id>ACCTMAINT</Id>
                          <Desc>Account maintenance fee</Desc>
                        </BkSvc>
                      </SvcDtl>
                      <Pric>
                        <UnitPric>
                          <Amt Ccy="EUR">15.00</Amt>
                          <Sgn>true</Sgn>
                        </UnitPric>
                      </Pric>
                      <PmtMtd>FLAT</PmtMtd>
                      <OrgnlChrgPric>
                        <Amt Ccy="EUR">15.00</Amt>
                        <Sgn>true</Sgn>
                      </OrgnlChrgPric>
                      <TaxDsgnt>
                        <Cd>TAXE</Cd>
                      </TaxDsgnt>
                    </Svc>
                  </BllgStmt>
                </BllgStmtGrp>
              </BkSvcsBllgStmt>
            </Document>
            """;

        var service = Camt086Reader.Read(xml).Groups[0].Statements[0].Services[0];

        service.UnitPrice.ShouldBe(new Money(-15.00m, "EUR"));
        service.OriginalChargePrice.ShouldBe(new Money(-15.00m, "EUR"));
    }

    [Fact]
    public void Read_Sample_ReadsTaxRegion()
    {
        var statement = CamtReaderTestHelpers.ReadCamt086Sample().Groups[0].Statements[0];

        statement.TaxRegions.Count.ShouldBe(1);
        var region = statement.TaxRegions[0];
        region.RegionNumber.ShouldBe("DE");
        region.RegionName.ShouldBe("Germany");
        region.CustomerTaxId.ShouldBe("DE123456789");
        region.SettlementAmount.ShouldBe(new Money(8.72m, "EUR"));
        region.TaxDueToRegion.ShouldBe(new Money(8.50m, "EUR"));
        region.Source.ShouldNotBeNull();
    }

    [Fact]
    public void Read_RootWithoutRecognizedChild_ThrowsValidationException()
    {
        const string xml = "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:camt.086.001.02\"><SomethingElse/></Document>";

        var exception = Should.Throw<Iso20022ValidationException>(() => Camt086Reader.Read(xml));
        exception.Message.ShouldContain("BkSvcsBllgStmt");
        exception.Message.ShouldContain("SomethingElse");
        exception.Path.ShouldBe("Document/BkSvcsBllgStmt");
    }

    [Fact]
    public void Read_RootNotNamedDocument_ThrowsValidationExceptionWithDocumentPath()
    {
        const string xml = "<Foo xmlns=\"urn:iso:std:iso:20022:tech:xsd:camt.086.001.02\"><BkSvcsBllgStmt><RptHdr><RptId>BILLRPT-1</RptId></RptHdr></BkSvcsBllgStmt></Foo>";

        var exception = Should.Throw<Iso20022ValidationException>(() => Camt086Reader.Read(xml));
        exception.Path.ShouldBe("Document");
    }

    [Fact]
    public void Read_BalanceWithoutAmt_ThrowsValidationExceptionWithPath()
    {
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.086.001.02">
              <BkSvcsBllgStmt>
                <BllgStmtGrp>
                  <BllgStmt>
                    <StmtId>BILLSTMT-1</StmtId>
                    <Bal>
                      <Tp><Cd>CLBD</Cd></Tp>
                      <Val>
                        <Sgn>true</Sgn>
                      </Val>
                    </Bal>
                  </BllgStmt>
                </BllgStmtGrp>
              </BkSvcsBllgStmt>
            </Document>
            """;

        var exception = Should.Throw<Iso20022ValidationException>(() => Camt086Reader.Read(xml));
        exception.Path.ShouldBe("BllgStmt/Bal/Val/Amt");
    }

    [Fact]
    public void Read_Camt086001_01Namespace_ParsesUsingLocalNames()
    {
        // The gateway's C86 mock message identifies as camt.086.001.01 rather than the documented
        // camt.086.001.02; the reader matches elements by local name regardless of namespace,
        // so both schema versions parse through the same code path.
        const string xml = """
            <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.086.001.01">
              <BkSvcsBllgStmt>
                <RptHdr>
                  <RptId>BILLRPT-1</RptId>
                </RptHdr>
                <BllgStmtGrp>
                  <GrpId>BILLGRP-1</GrpId>
                  <BllgStmt>
                    <StmtId>BILLSTMT-1</StmtId>
                    <AcctChrtcs>
                      <CshAcct>
                        <Id>
                          <IBAN>DE89370400440532013000</IBAN>
                        </Id>
                      </CshAcct>
                    </AcctChrtcs>
                  </BllgStmt>
                </BllgStmtGrp>
              </BkSvcsBllgStmt>
            </Document>
            """;

        var message = Camt086Reader.Read(xml);

        message.Identifier.Type.ShouldBe(Iso20022MessageType.Camt086);
        message.Identifier.Identifier.ShouldBe("camt.086.001.01");
        var statement = message.Groups[0].Statements[0];
        statement.StatementId.ShouldBe("BILLSTMT-1");
        statement.Account.ShouldNotBeNull();
        statement.Account!.Iban.ShouldBe("DE89370400440532013000");
    }

    [Fact]
    public void Read_StreamAndString_ProduceEquivalentResults()
    {
        var xml = SampleXml.Load("camt.086.001.02-sample.xml");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var fromString = Camt086Reader.Read(xml);
        var fromStream = Camt086Reader.Read(stream);

        fromStream.ReportId.ShouldBe(fromString.ReportId);
        fromStream.Groups[0].Statements[0].Services.Count.ShouldBe(fromString.Groups[0].Statements[0].Services.Count);
    }
}

internal static class CamtReaderTestHelpers
{
    public static BankServicesBillingMessage ReadCamt086Sample() =>
        Camt086Reader.Read(SampleXml.Load("camt.086.001.02-sample.xml"));
}
