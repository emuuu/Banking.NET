using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Iso20022.Common;

public class CommonReadersTests
{
    private static XElement Parse(string xml) => XElement.Parse(xml);

    [Fact]
    public void ReadParty_Null_ReturnsNull()
    {
        CommonReaders.ReadParty(null).ShouldBeNull();
    }

    [Fact]
    public void ReadParty_WithPtyWrapper_ResolvesTransparently()
    {
        var element = Parse("<Dbtr><Pty><Nm>Example Debtor GmbH</Nm></Pty></Dbtr>");
        var party = CommonReaders.ReadParty(element);

        party.ShouldNotBeNull();
        party!.Name.ShouldBe("Example Debtor GmbH");
        party.Source.ShouldBe(element);
    }

    [Fact]
    public void ReadParty_WithoutPtyWrapper_ReadsDirectly()
    {
        var element = Parse("<Dbtr><Nm>Example Debtor GmbH</Nm></Dbtr>");
        var party = CommonReaders.ReadParty(element);

        party!.Name.ShouldBe("Example Debtor GmbH");
    }

    [Fact]
    public void ReadParty_WithOrganisationId_ReadsBicAndOtherScheme()
    {
        var element = Parse("""
            <Dbtr>
              <Nm>Example Debtor GmbH</Nm>
              <Id>
                <OrgId>
                  <AnyBIC>COBADEFFXXX</AnyBIC>
                  <Othr>
                    <Id>HRB12345</Id>
                    <SchmeNm><Cd>CRID</Cd></SchmeNm>
                    <Issr>Local Register</Issr>
                  </Othr>
                </OrgId>
              </Id>
            </Dbtr>
            """);

        var party = CommonReaders.ReadParty(element);

        party!.OrganisationId.ShouldNotBeNull();
        party.OrganisationId!.Bic.ShouldBe("COBADEFFXXX");
        party.OrganisationId.OtherId.ShouldBe("HRB12345");
        party.OrganisationId.OtherSchemeCode.ShouldBe("CRID");
        party.OrganisationId.OtherIssuer.ShouldBe("Local Register");
    }

    [Fact]
    public void ReadParty_WithPrivateId_ReadsBirthData()
    {
        var element = Parse("""
            <Dbtr>
              <Nm>John Doe</Nm>
              <Id>
                <PrvtId>
                  <DtAndPlcOfBirth>
                    <BirthDt>1980-05-15</BirthDt>
                    <CityOfBirth>Berlin</CityOfBirth>
                    <CtryOfBirth>DE</CtryOfBirth>
                  </DtAndPlcOfBirth>
                </PrvtId>
              </Id>
            </Dbtr>
            """);

        var party = CommonReaders.ReadParty(element);

        party!.PrivateId.ShouldNotBeNull();
        party.PrivateId!.BirthDate.ShouldBe(new DateOnly(1980, 5, 15));
        party.PrivateId.CityOfBirth.ShouldBe("Berlin");
        party.PrivateId.CountryOfBirth.ShouldBe("DE");
    }

    [Fact]
    public void ReadPostalAddress_AllFields_ReadsAll()
    {
        var element = Parse("""
            <PstlAdr>
              <Dept>Finance</Dept>
              <StrtNm>Kaiserstrasse</StrtNm>
              <BldgNb>16</BldgNb>
              <PstCd>60311</PstCd>
              <TwnNm>Frankfurt am Main</TwnNm>
              <CtrySubDvsn>Hessen</CtrySubDvsn>
              <Ctry>DE</Ctry>
              <AdrLine>Line 1</AdrLine>
              <AdrLine>Line 2</AdrLine>
            </PstlAdr>
            """);

        var address = CommonReaders.ReadPostalAddress(element);

        address!.Department.ShouldBe("Finance");
        address.StreetName.ShouldBe("Kaiserstrasse");
        address.BuildingNumber.ShouldBe("16");
        address.PostCode.ShouldBe("60311");
        address.TownName.ShouldBe("Frankfurt am Main");
        address.CountrySubDivision.ShouldBe("Hessen");
        address.Country.ShouldBe("DE");
        address.AddressLines.ShouldBe(["Line 1", "Line 2"]);
    }

    [Fact]
    public void ReadAccount_Iban_ReadsIban()
    {
        var element = Parse("<Acct><Id><IBAN>DE89370400440532013000</IBAN></Id><Ccy>EUR</Ccy></Acct>");
        var account = CommonReaders.ReadAccount(element);

        account!.Iban.ShouldBe("DE89370400440532013000");
        account.Currency.ShouldBe("EUR");
        account.OtherId.ShouldBeNull();
    }

    [Fact]
    public void ReadAccount_OtherId_ReadsOtherIdAndScheme()
    {
        var element = Parse("<Acct><Id><Othr><Id>ACCT-1</Id><SchmeNm><Cd>BBAN</Cd></SchmeNm></Othr></Id></Acct>");
        var account = CommonReaders.ReadAccount(element);

        account!.Iban.ShouldBeNull();
        account.OtherId.ShouldBe("ACCT-1");
        account.OtherSchemeCode.ShouldBe("BBAN");
    }

    [Fact]
    public void ReadFinancialInstitution_Bic_ReadsBic()
    {
        var element = Parse("<DbtrAgt><FinInstnId><BIC>COBADEFFXXX</BIC><Nm>Commerzbank AG</Nm></FinInstnId></DbtrAgt>");
        var institution = CommonReaders.ReadFinancialInstitution(element);

        institution!.Bic.ShouldBe("COBADEFFXXX");
        institution.Name.ShouldBe("Commerzbank AG");
    }

    [Fact]
    public void ReadFinancialInstitution_Bicfi_ReadsBic()
    {
        var element = Parse("<DbtrAgt><FinInstnId><BICFI>COBADEFFXXX</BICFI></FinInstnId></DbtrAgt>");
        var institution = CommonReaders.ReadFinancialInstitution(element);

        institution!.Bic.ShouldBe("COBADEFFXXX");
    }

    [Fact]
    public void ReadFinancialInstitution_Null_ReturnsNull()
    {
        CommonReaders.ReadFinancialInstitution(null).ShouldBeNull();
    }

    [Fact]
    public void ReadRemittance_Unstructured_ReadsLines()
    {
        var element = Parse("<RmtInf><Ustrd>Invoice 1</Ustrd><Ustrd>Invoice 2</Ustrd></RmtInf>");
        var remittance = CommonReaders.ReadRemittance(element);

        remittance!.Unstructured.ShouldBe(["Invoice 1", "Invoice 2"]);
        remittance.CreditorReference.ShouldBeNull();
    }

    [Fact]
    public void ReadRemittance_StructuredScor_ReadsCreditorReference()
    {
        var element = Parse("""
            <RmtInf>
              <Strd>
                <CdtrRefInf>
                  <Tp><CdOrPrtry><Cd>SCOR</Cd></CdOrPrtry><Issr>Example Creditor Ltd</Issr></Tp>
                  <Ref>RF18539007547034</Ref>
                </CdtrRefInf>
              </Strd>
            </RmtInf>
            """);

        var remittance = CommonReaders.ReadRemittance(element);

        remittance!.CreditorReference.ShouldBe("RF18539007547034");
        remittance.CreditorReferenceTypeCode.ShouldBe("SCOR");
        remittance.CreditorReferenceIssuer.ShouldBe("Example Creditor Ltd");
    }

    [Fact]
    public void ReadRemittance_MultipleStructuredElements_UsesFirstWithCreditorReference()
    {
        var element = Parse("""
            <RmtInf>
              <Strd>
                <RfrdDocInf><Tp><CdOrPrtry><Cd>CINV</Cd></CdOrPrtry></Tp></RfrdDocInf>
              </Strd>
              <Strd>
                <CdtrRefInf>
                  <Tp><CdOrPrtry><Cd>SCOR</Cd></CdOrPrtry><Issr>Example Creditor Ltd</Issr></Tp>
                  <Ref>RF18539007547034</Ref>
                </CdtrRefInf>
              </Strd>
            </RmtInf>
            """);

        var remittance = CommonReaders.ReadRemittance(element);

        remittance!.CreditorReference.ShouldBe("RF18539007547034");
        remittance.CreditorReferenceTypeCode.ShouldBe("SCOR");
        remittance.CreditorReferenceIssuer.ShouldBe("Example Creditor Ltd");
    }

    [Fact]
    public void ReadStatusReasons_MultipleEntries_ReadsAll()
    {
        var elements = new[]
        {
            Parse("<StsRsnInf><Rsn><Cd>AC04</Cd></Rsn></StsRsnInf>"),
            Parse("<StsRsnInf><Rsn><Prtry>CUST</Prtry></Rsn><AddtlInf>Details</AddtlInf></StsRsnInf>"),
        };

        var reasons = CommonReaders.ReadStatusReasons(elements);

        reasons.Count.ShouldBe(2);
        reasons[0].Code.ShouldBe("AC04");
        reasons[1].Proprietary.ShouldBe("CUST");
        reasons[1].AdditionalInformation.ShouldBe(["Details"]);
    }

    [Fact]
    public void ReadBankTransactionCode_DomainFamily_ReadsAll()
    {
        var element = Parse("<BkTxCd><Domn><Cd>PMNT</Cd><Fmly><Cd>RCDT</Cd><SubFmlyCd>ESCT</SubFmlyCd></Fmly></Domn></BkTxCd>");
        var code = CommonReaders.ReadBankTransactionCode(element);

        code!.Domain.ShouldBe("PMNT");
        code.Family.ShouldBe("RCDT");
        code.SubFamily.ShouldBe("ESCT");
    }

    [Fact]
    public void ReadBankTransactionCode_Proprietary_ReadsProprietary()
    {
        var element = Parse("<BkTxCd><Prtry><Cd>XYZ</Cd><Issr>Commerzbank</Issr></Prtry></BkTxCd>");
        var code = CommonReaders.ReadBankTransactionCode(element);

        code!.ProprietaryCode.ShouldBe("XYZ");
        code.ProprietaryIssuer.ShouldBe("Commerzbank");
    }

    [Fact]
    public void ReadPaymentTypeInformation_AllFields_ReadsAll()
    {
        var element = Parse("""
            <PmtTpInf>
              <InstrPrty>NORM</InstrPrty>
              <SvcLvl><Cd>SEPA</Cd></SvcLvl>
              <LclInstrm><Cd>CORE</Cd></LclInstrm>
              <SeqTp>FRST</SeqTp>
              <CtgyPurp><Cd>SUPP</Cd></CtgyPurp>
            </PmtTpInf>
            """);

        var info = CommonReaders.ReadPaymentTypeInformation(element);

        info!.InstructionPriority.ShouldBe("NORM");
        info.ServiceLevelCode.ShouldBe("SEPA");
        info.LocalInstrumentCode.ShouldBe("CORE");
        info.SequenceType.ShouldBe("FRST");
        info.CategoryPurposeCode.ShouldBe("SUPP");
    }

    [Fact]
    public void ReadMandate_WithAmendment_ReadsOriginalValues()
    {
        var element = Parse("""
            <MndtRltdInf>
              <MndtId>MANDATE-1</MndtId>
              <DtOfSgntr>2026-01-15</DtOfSgntr>
              <AmdmntInd>true</AmdmntInd>
              <AmdmntInfDtls>
                <OrgnlMndtId>MANDATE-0</OrgnlMndtId>
                <OrgnlCdtrSchmeId>
                  <Nm>Old Creditor</Nm>
                  <Id><PrvtId><Othr><Id>DE00OLD</Id></Othr></PrvtId></Id>
                </OrgnlCdtrSchmeId>
                <OrgnlDbtrAcct><Id><IBAN>DE02120300000000202051</IBAN></Id></OrgnlDbtrAcct>
              </AmdmntInfDtls>
            </MndtRltdInf>
            """);

        var mandate = CommonReaders.ReadMandate(element);

        mandate!.MandateId.ShouldBe("MANDATE-1");
        mandate.DateOfSignature.ShouldBe(new DateOnly(2026, 1, 15));
        mandate.AmendmentIndicator.ShouldBe(true);
        mandate.OriginalMandateId.ShouldBe("MANDATE-0");
        mandate.OriginalCreditorName.ShouldBe("Old Creditor");
        mandate.OriginalCreditorSchemeId.ShouldBe("DE00OLD");
        mandate.OriginalDebtorAccount.ShouldNotBeNull();
        mandate.OriginalDebtorAccount!.Iban.ShouldBe("DE02120300000000202051");
    }

    [Fact]
    public void ReadMandate_WithoutAmendment_LeavesOriginalValuesNull()
    {
        var element = Parse("<MndtRltdInf><MndtId>MANDATE-1</MndtId><DtOfSgntr>2026-01-15</DtOfSgntr></MndtRltdInf>");
        var mandate = CommonReaders.ReadMandate(element);

        mandate!.AmendmentIndicator.ShouldBeNull();
        mandate.OriginalMandateId.ShouldBeNull();
        mandate.OriginalDebtorAccount.ShouldBeNull();
    }

    [Theory]
    [InlineData("CRDT", CreditDebitIndicator.Credit)]
    [InlineData("DBIT", CreditDebitIndicator.Debit)]
    public void ReadCreditDebit_ValidValue_ReturnsIndicator(string value, CreditDebitIndicator expected)
    {
        CommonReaders.ReadCreditDebit(Parse($"<CdtDbtInd>{value}</CdtDbtInd>")).ShouldBe(expected);
    }

    [Fact]
    public void ReadCreditDebit_Null_ReturnsNull()
    {
        CommonReaders.ReadCreditDebit(null).ShouldBeNull();
    }

    [Fact]
    public void ReadCreditDebit_InvalidValue_ThrowsFormatException()
    {
        var element = Parse("<CdtDbtInd>MAYBE</CdtDbtInd>");
        Should.Throw<FormatException>(() => CommonReaders.ReadCreditDebit(element));
    }
}
