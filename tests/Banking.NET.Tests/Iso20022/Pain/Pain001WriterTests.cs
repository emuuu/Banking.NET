using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Iso20022.Pain;

public class Pain001WriterTests
{
    private static readonly string[] MinimalElementSequence =
    [
        "Document", "CstmrCdtTrfInitn", "GrpHdr", "MsgId", "CreDtTm", "NbOfTxs", "CtrlSum", "InitgPty", "Nm",
        "PmtInf", "PmtInfId", "PmtMtd", "NbOfTxs", "CtrlSum", "PmtTpInf", "SvcLvl", "Cd", "ReqdExctnDt",
        "Dbtr", "Nm", "DbtrAcct", "Id", "IBAN", "DbtrAgt", "FinInstnId", "Othr", "Id", "ChrgBr",
        "CdtTrfTxInf", "PmtId", "EndToEndId", "Amt", "InstdAmt", "Cdtr", "Nm", "CdtrAcct", "Id", "IBAN",
    ];

    private static CreditTransferInitiation MinimalInitiation() => new()
    {
        MessageId = "MSGID-0001",
        InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
        PaymentInformations =
        [
            new CreditTransferPaymentInformation
            {
                PaymentInformationId = "PMTINF-0001",
                RequestedExecutionDate = new DateOnly(2026, 9, 15),
                Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                Transactions =
                [
                    new CreditTransferTransaction
                    {
                        EndToEndId = "E2E-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                        CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                    },
                ],
            },
        ],
    };

    [Fact]
    public void Write_MinimalV03_ProducesExpectedElementSequenceAndNamespace()
    {
        var document = Pain001Writer.Write(MinimalInitiation(), Pain001Version.V03);

        document.Root.ShouldNotBeNull();
        document.Root!.Name.NamespaceName.ShouldBe("urn:iso:std:iso:20022:tech:xsd:pain.001.001.03");
        document.Descendants().Select(e => e.Name.LocalName).ShouldBe(MinimalElementSequence);
    }

    [Fact]
    public void Write_MinimalV09_UsesDtChoiceForRequestedExecutionDateAndCorrectNamespace()
    {
        var document = Pain001Writer.Write(MinimalInitiation(), Pain001Version.V09);

        document.Root!.Name.NamespaceName.ShouldBe("urn:iso:std:iso:20022:tech:xsd:pain.001.001.09");
        var ns = document.Root!.Name.Namespace;
        var reqdExctnDt = document.Descendants(ns + "ReqdExctnDt").Single();
        reqdExctnDt.Elements(ns + "Dt").Single().Value.ShouldBe("2026-09-15");
    }

    [Theory]
    [InlineData(12.3, "12.30")]
    [InlineData(12.34, "12.34")]
    [InlineData(100, "100.00")]
    public void Write_Amount_FormatsWithExactlyTwoDecimalPlaces(decimal amount, string expected)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(amount, "EUR");

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "InstdAmt").Single().Value.ShouldBe(expected);
    }

    [Fact]
    public void Write_MultiplePaymentInformationsAndTransactions_ComputesTotalsAtEachLevel()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions.Add(new CreditTransferTransaction
        {
            EndToEndId = "E2E-0002",
            Amount = new Money(50.00m, "EUR"),
            Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
            CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
        });
        initiation.PaymentInformations.Add(new CreditTransferPaymentInformation
        {
            PaymentInformationId = "PMTINF-0002",
            RequestedExecutionDate = new DateOnly(2026, 9, 16),
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
            Transactions = [new CreditTransferTransaction
            {
                EndToEndId = "E2E-0003",
                Amount = new Money(25.00m, "EUR"),
                Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
            }],
        });

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var groupHeader = document.Descendants(ns + "GrpHdr").Single();
        groupHeader.Element(ns + "NbOfTxs")!.Value.ShouldBe("3");
        groupHeader.Element(ns + "CtrlSum")!.Value.ShouldBe("175.00");

        var paymentInfos = document.Descendants(ns + "PmtInf").ToList();
        paymentInfos[0].Element(ns + "NbOfTxs")!.Value.ShouldBe("2");
        paymentInfos[0].Element(ns + "CtrlSum")!.Value.ShouldBe("150.00");
        paymentInfos[1].Element(ns + "NbOfTxs")!.Value.ShouldBe("1");
        paymentInfos[1].Element(ns + "CtrlSum")!.Value.ShouldBe("25.00");
    }

    [Theory]
    [InlineData(Pain001Version.V03, "BIC")]
    [InlineData(Pain001Version.V09, "BICFI")]
    public void Write_DebtorAgentWithBic_WritesVersionSpecificBicElement(Pain001Version version, string expectedElementName)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" };

        var document = Pain001Writer.Write(initiation, version);

        var ns = document.Root!.Name.Namespace;
        var debtorAgent = document.Descendants(ns + "DbtrAgt").Single();
        debtorAgent.Descendants(ns + expectedElementName).Single().Value.ShouldBe("COBADEFFXXX");
        debtorAgent.Descendants(ns + "Othr").ShouldBeEmpty();
    }

    [Fact]
    public void Write_DebtorAccountWithOnlyOtherId_WritesOthrId()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { OtherId = "OTHER-ACCT-0001" };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var debtorAccount = document.Descendants(ns + "DbtrAcct").Single();
        debtorAccount.Descendants(ns + "Othr").Single().Element(ns + "Id")!.Value.ShouldBe("OTHER-ACCT-0001");
        debtorAccount.Descendants(ns + "IBAN").ShouldBeEmpty();
    }

    [Fact]
    public void Write_DebtorAgentWithoutBic_WritesNotProvidedFallback()
    {
        var document = Pain001Writer.Write(MinimalInitiation(), Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var debtorAgent = document.Descendants(ns + "DbtrAgt").Single();
        debtorAgent.Descendants(ns + "Othr").Single().Element(ns + "Id")!.Value.ShouldBe("NOTPROVIDED");
    }

    [Fact]
    public void Write_CreditorAgentWithBic_WritesCreditorAgentElement()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].CreditorAgent = new FinancialInstitution { Bic = "MARKDEF1100" };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "CdtrAgt").Single().Descendants(ns + "BIC").Single().Value.ShouldBe("MARKDEF1100");
    }

    [Fact]
    public void Write_CreditorAgentWithoutBic_OmitsOptionalCreditorAgentElement()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].CreditorAgent = new FinancialInstitution();

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "CdtrAgt").ShouldBeEmpty();
    }

    [Fact]
    public void Write_PostalAddress_WritesFieldsInSchemaOrder()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Debtor.PostalAddress = new PostalAddress
        {
            Department = "Finance",
            StreetName = "Kaiserplatz",
            BuildingNumber = "1",
            PostCode = "60311",
            TownName = "Frankfurt am Main",
            CountrySubDivision = "HE",
            Country = "DE",
            AddressLines = ["Building A", "Floor 3"],
        };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var address = document.Descendants(ns + "Dbtr").Single().Element(ns + "PstlAdr")!;
        address.Elements().Select(e => e.Name.LocalName).ShouldBe(
            ["Dept", "StrtNm", "BldgNb", "PstCd", "TwnNm", "CtrySubDvsn", "Ctry", "AdrLine", "AdrLine"]);
        address.Elements(ns + "AdrLine").Select(e => e.Value).ShouldBe(["Building A", "Floor 3"]);
    }

    [Theory]
    [InlineData(Pain001Version.V03, "BICOrBEI")]
    [InlineData(Pain001Version.V09, "AnyBIC")]
    public void Write_OrganisationId_WritesVersionSpecificBicChoiceAndOtherIdentification(Pain001Version version, string expectedBicElementName)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Debtor.OrganisationId = new OrganisationIdentification
        {
            Bic = "COBADEFFXXX",
            OtherId = "HRB12345",
            OtherSchemeCode = "COID",
            OtherIssuer = "Handelsregister",
        };

        var document = Pain001Writer.Write(initiation, version);

        var ns = document.Root!.Name.Namespace;
        var orgId = document.Descendants(ns + "Dbtr").Single().Element(ns + "Id")!.Element(ns + "OrgId")!;
        orgId.Element(ns + expectedBicElementName)!.Value.ShouldBe("COBADEFFXXX");
        var other = orgId.Element(ns + "Othr")!;
        other.Element(ns + "Id")!.Value.ShouldBe("HRB12345");
        other.Element(ns + "SchmeNm")!.Element(ns + "Cd")!.Value.ShouldBe("COID");
        other.Element(ns + "Issr")!.Value.ShouldBe("Handelsregister");
    }

    [Fact]
    public void Write_PrivateId_WritesBirthDataAndOtherIdentification()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Debtor.PrivateId = new PrivateIdentification
        {
            BirthDate = new DateOnly(1980, 5, 20),
            CityOfBirth = "Frankfurt am Main",
            CountryOfBirth = "DE",
            OtherId = "P-12345",
        };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var prvtId = document.Descendants(ns + "Dbtr").Single().Element(ns + "Id")!.Element(ns + "PrvtId")!;
        var birth = prvtId.Element(ns + "DtAndPlcOfBirth")!;
        birth.Element(ns + "BirthDt")!.Value.ShouldBe("1980-05-20");
        birth.Element(ns + "CityOfBirth")!.Value.ShouldBe("Frankfurt am Main");
        birth.Element(ns + "CtryOfBirth")!.Value.ShouldBe("DE");
        prvtId.Element(ns + "Othr")!.Element(ns + "Id")!.Value.ShouldBe("P-12345");
    }

    [Fact]
    public void Write_UnstructuredRemittanceInformation_WritesUstrd()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = ["Invoice 2026-0920"],
        };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var remittance = document.Descendants(ns + "RmtInf").Single();
        remittance.Elements(ns + "Ustrd").Single().Value.ShouldBe("Invoice 2026-0920");
    }

    [Fact]
    public void Write_StructuredRemittanceInformation_WritesCreditorReferenceWithDefaultScorCode()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = "RF18539007547034",
        };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var creditorRefInfo = document.Descendants(ns + "CdtrRefInf").Single();
        creditorRefInfo.Element(ns + "Tp")!.Element(ns + "CdOrPrtry")!.Element(ns + "Cd")!.Value.ShouldBe("SCOR");
        creditorRefInfo.Element(ns + "Ref")!.Value.ShouldBe("RF18539007547034");
    }

    [Fact]
    public void Write_StructuredRemittanceInformationWithCustomTypeCodeAndIssuer_WritesGivenValues()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = "RF18539007547034",
            CreditorReferenceTypeCode = "RADM",
            CreditorReferenceIssuer = "Example Issuer",
        };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var creditorRefInfo = document.Descendants(ns + "CdtrRefInf").Single();
        creditorRefInfo.Element(ns + "Tp")!.Element(ns + "CdOrPrtry")!.Element(ns + "Cd")!.Value.ShouldBe("RADM");
        creditorRefInfo.Element(ns + "Tp")!.Element(ns + "Issr")!.Value.ShouldBe("Example Issuer");
        creditorRefInfo.Element(ns + "Ref")!.Value.ShouldBe("RF18539007547034");
    }

    [Theory]
    [InlineData(Pain001Version.V03, "pain.001.001.03.xsd")]
    [InlineData(Pain001Version.V09, "pain.001.001.09.xsd")]
    public void Write_CreditorReferenceTypeCodeWithoutReference_WritesTpWithoutRefAndValidatesAgainstSchema(Pain001Version version, string schemaFileName)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeCode = "SCOR",
        };

        var xml = Pain001Writer.WriteToString(initiation, version);
        var document = XDocument.Parse(xml);

        var ns = document.Root!.Name.Namespace;
        var creditorRefInfo = document.Descendants(ns + "CdtrRefInf").Single();
        creditorRefInfo.Element(ns + "Tp")!.Element(ns + "CdOrPrtry")!.Element(ns + "Cd")!.Value.ShouldBe("SCOR");
        creditorRefInfo.Element(ns + "Ref").ShouldBeNull();

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Fact]
    public void Write_UltimateDebtorAndCreditor_WritesAtPaymentInformationAndTransactionLevel()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Group" };
        initiation.PaymentInformations[0].Transactions[0].UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Sub" };
        initiation.PaymentInformations[0].Transactions[0].UltimateCreditor = new PartyIdentification { Name = "Ultimate Creditor Group" };

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "PmtInf").Single().Element(ns + "UltmtDbtr")!.Element(ns + "Nm")!.Value.ShouldBe("Ultimate Debtor Group");
        var transaction = document.Descendants(ns + "CdtTrfTxInf").Single();
        transaction.Element(ns + "UltmtDbtr")!.Element(ns + "Nm")!.Value.ShouldBe("Ultimate Debtor Sub");
        transaction.Element(ns + "UltmtCdtr")!.Element(ns + "Nm")!.Value.ShouldBe("Ultimate Creditor Group");
    }

    [Fact]
    public void Write_PurposeCode_WritesPurpCd()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].PurposeCode = "SUPP";

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "Purp").Single().Element(ns + "Cd")!.Value.ShouldBe("SUPP");
    }

    [Fact]
    public void Write_InstructionPriorityAndLocalInstrument_WriteInPaymentTypeInformation()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].InstructionPriority = InstructionPriority.High;
        initiation.PaymentInformations[0].LocalInstrumentCode = "INST";

        var document = Pain001Writer.Write(initiation, Pain001Version.V03);

        var ns = document.Root!.Name.Namespace;
        var paymentTypeInformation = document.Descendants(ns + "PmtTpInf").Single();
        paymentTypeInformation.Element(ns + "InstrPrty")!.Value.ShouldBe("HIGH");
        paymentTypeInformation.Element(ns + "LclInstrm")!.Element(ns + "Cd")!.Value.ShouldBe("INST");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteToString_OmitXmlDeclaration_ControlsDeclarationPresence(bool omit)
    {
        var xml = Pain001Writer.WriteToString(MinimalInitiation(), Pain001Version.V03, new Pain00xWriterOptions { OmitXmlDeclaration = omit });

        xml.StartsWith("<?xml", StringComparison.Ordinal).ShouldBe(!omit);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_OmitXmlDeclaration_ControlsDocumentDeclaration(bool omit)
    {
        var document = Pain001Writer.Write(MinimalInitiation(), Pain001Version.V03, new Pain00xWriterOptions { OmitXmlDeclaration = omit });

        if (omit)
            document.Declaration.ShouldBeNull();
        else
            document.Declaration.ShouldNotBeNull();
    }

    [Fact]
    public void Write_UndefinedVersion_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Pain001Writer.Write(MinimalInitiation(), (Pain001Version)99));
    }

    [Fact]
    public void WriteToString_ProducesUtf8WithoutByteOrderMark()
    {
        var bytes = Pain001Writer.WriteToBytes(MinimalInitiation(), Pain001Version.V03);

        bytes[0].ShouldNotBe((byte)0xEF);
    }

    [Theory]
    [InlineData(Pain001Version.V03, "pain.001.001.03")]
    [InlineData(Pain001Version.V09, "pain.001.001.09")]
    public void WriteToString_RoundtripsThroughIdentify(Pain001Version version, string expectedIdentifier)
    {
        var xml = Pain001Writer.WriteToString(MinimalInitiation(), version);

        var identifier = Iso20022Document.Identify(Iso20022Document.Load(xml));

        identifier.Type.ShouldBe(Iso20022MessageType.Pain001);
        identifier.Identifier.ShouldBe(expectedIdentifier);
    }

    [Fact]
    public void Write_NoPaymentInformations_ThrowsValidationException()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations.Clear();

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain001Writer.Write(initiation, Pain001Version.V03));

        exception.Path.ShouldBe("PaymentInformations");
    }

    [Fact]
    public void Write_NullInitiation_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Pain001Writer.Write(null!, Pain001Version.V03));
    }

    [Fact]
    public void Write_ToStream_WritesSameContentAsWriteToBytes()
    {
        using var stream = new MemoryStream();

        Pain001Writer.Write(MinimalInitiation(), Pain001Version.V03, stream);

        stream.ToArray().ShouldBe(Pain001Writer.WriteToBytes(MinimalInitiation(), Pain001Version.V03));
    }
}
