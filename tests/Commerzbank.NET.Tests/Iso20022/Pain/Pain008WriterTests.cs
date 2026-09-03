using System.Xml.Linq;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Pain;

public class Pain008WriterTests
{
    private static readonly string[] MinimalElementSequence =
    [
        "Document", "CstmrDrctDbtInitn", "GrpHdr", "MsgId", "CreDtTm", "NbOfTxs", "CtrlSum", "InitgPty", "Nm",
        "PmtInf", "PmtInfId", "PmtMtd", "NbOfTxs", "CtrlSum", "PmtTpInf", "SvcLvl", "Cd", "LclInstrm", "Cd", "SeqTp",
        "ReqdColltnDt", "Cdtr", "Nm", "CdtrAcct", "Id", "IBAN", "CdtrAgt", "FinInstnId", "Othr", "Id", "ChrgBr",
        "CdtrSchmeId", "Id", "PrvtId", "Othr", "Id", "SchmeNm", "Prtry",
        "DrctDbtTxInf", "PmtId", "EndToEndId", "InstdAmt", "DrctDbtTx", "MndtRltdInf", "MndtId", "DtOfSgntr",
        "DbtrAgt", "FinInstnId", "Othr", "Id", "Dbtr", "Nm", "DbtrAcct", "Id", "IBAN",
    ];

    private static DirectDebitInitiation MinimalInitiation() => new()
    {
        MessageId = "MSGID-0001",
        InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
        PaymentInformations =
        [
            new DirectDebitPaymentInformation
            {
                PaymentInformationId = "PMTINF-0001",
                Scheme = DirectDebitScheme.Core,
                SequenceType = SequenceType.Recurring,
                RequestedCollectionDate = new DateOnly(2026, 9, 15),
                Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                CreditorSchemeId = "DE98ZZZ09999999999",
                Transactions =
                [
                    new DirectDebitTransaction
                    {
                        EndToEndId = "E2E-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Mandate = new MandateInformation { MandateId = "MANDATE-0001", DateOfSignature = new DateOnly(2026, 1, 10) },
                        Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                        DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                    },
                ],
            },
        ],
    };

    [Fact]
    public void Write_MinimalV02_ProducesExpectedElementSequenceAndNamespace()
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V02);

        document.Root!.Name.NamespaceName.ShouldBe("urn:iso:std:iso:20022:tech:xsd:pain.008.001.02");
        document.Descendants().Select(e => e.Name.LocalName).ShouldBe(MinimalElementSequence);
    }

    [Fact]
    public void Write_MinimalV08_UsesCorrectNamespace()
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V08);

        document.Root!.Name.NamespaceName.ShouldBe("urn:iso:std:iso:20022:tech:xsd:pain.008.001.08");
        document.Descendants().Select(e => e.Name.LocalName).ShouldBe(MinimalElementSequence);
    }

    [Theory]
    [InlineData(DirectDebitScheme.Core, "CORE")]
    [InlineData(DirectDebitScheme.B2B, "B2B")]
    public void Write_Scheme_WritesLocalInstrumentCode(DirectDebitScheme scheme, string expectedCode)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Scheme = scheme;

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "LclInstrm").Single().Element(ns + "Cd")!.Value.ShouldBe(expectedCode);
    }

    [Theory]
    [InlineData(SequenceType.First, "FRST")]
    [InlineData(SequenceType.Recurring, "RCUR")]
    [InlineData(SequenceType.OneOff, "OOFF")]
    [InlineData(SequenceType.Final, "FNAL")]
    public void Write_SequenceType_WritesSeqTpCode(SequenceType sequenceType, string expectedCode)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].SequenceType = sequenceType;

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "SeqTp").Single().Value.ShouldBe(expectedCode);
    }

    [Fact]
    public void Write_CreditorSchemeId_WritesPrivateIdentificationWithSepaProprietaryScheme()
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        var creditorSchemeId = document.Descendants(ns + "CdtrSchmeId").Single();
        var other = creditorSchemeId.Element(ns + "Id")!.Element(ns + "PrvtId")!.Element(ns + "Othr")!;
        other.Element(ns + "Id")!.Value.ShouldBe("DE98ZZZ09999999999");
        other.Element(ns + "SchmeNm")!.Element(ns + "Prtry")!.Value.ShouldBe("SEPA");
    }

    [Theory]
    [InlineData(Pain008Version.V02, "BIC")]
    [InlineData(Pain008Version.V08, "BICFI")]
    public void Write_CreditorAgentWithBic_WritesVersionSpecificBicElement(Pain008Version version, string expectedElementName)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" };

        var document = Pain008Writer.Write(initiation, version);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "CdtrAgt").Single().Descendants(ns + expectedElementName).Single().Value.ShouldBe("COBADEFFXXX");
    }

    [Fact]
    public void Write_DebtorAgentWithoutBic_WritesNotProvidedFallback()
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "DbtrAgt").Single().Descendants(ns + "Othr").Single().Element(ns + "Id")!.Value.ShouldBe("NOTPROVIDED");
    }

    [Fact]
    public void Write_MandateWithoutAmendment_OmitsAmendmentElements()
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        var mandate = document.Descendants(ns + "MndtRltdInf").Single();
        mandate.Elements(ns + "AmdmntInd").ShouldBeEmpty();
        mandate.Elements(ns + "AmdmntInfDtls").ShouldBeEmpty();
    }

    [Fact]
    public void Write_MandateWithAmendmentButNoOriginalDebtorAgent_WritesEmptyAmendmentDetails()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].Mandate.AmendmentIndicator = true;

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        var mandate = document.Descendants(ns + "MndtRltdInf").Single();
        mandate.Element(ns + "AmdmntInd")!.Value.ShouldBe("true");
        mandate.Element(ns + "AmdmntInfDtls").ShouldNotBeNull();
        mandate.Element(ns + "AmdmntInfDtls")!.Elements(ns + "OrgnlDbtrAgt").ShouldBeEmpty();
    }

    [Fact]
    public void Write_MandateAmendmentWithOriginalDebtorAgentWithoutBic_WritesSmndaFallback()
    {
        var initiation = MinimalInitiation();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalMandateId = "MANDATE-OLD-0001";
        mandate.OriginalCreditorName = "Old Creditor Name Ltd";
        mandate.OriginalCreditorSchemeId = "DE98ZZZ00000000001";
        mandate.OriginalDebtorAccount = new AccountIdentification { Iban = "DE75512108001245126199" };
        mandate.OriginalDebtorAgent = new FinancialInstitution();

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        var amendmentDetails = document.Descendants(ns + "AmdmntInfDtls").Single();
        amendmentDetails.Element(ns + "OrgnlMndtId")!.Value.ShouldBe("MANDATE-OLD-0001");

        var originalCreditorSchemeId = amendmentDetails.Element(ns + "OrgnlCdtrSchmeId")!;
        originalCreditorSchemeId.Element(ns + "Nm")!.Value.ShouldBe("Old Creditor Name Ltd");
        var originalCreditorSchemeIdOther = originalCreditorSchemeId.Element(ns + "Id")!.Element(ns + "PrvtId")!.Element(ns + "Othr")!;
        originalCreditorSchemeIdOther.Element(ns + "Id")!.Value.ShouldBe("DE98ZZZ00000000001");
        originalCreditorSchemeIdOther.Element(ns + "SchmeNm")!.Element(ns + "Prtry")!.Value.ShouldBe("SEPA");

        amendmentDetails.Element(ns + "OrgnlDbtrAcct")!.Element(ns + "Id")!.Element(ns + "IBAN")!.Value.ShouldBe("DE75512108001245126199");

        var originalDebtorAgent = amendmentDetails.Element(ns + "OrgnlDbtrAgt")!.Element(ns + "FinInstnId")!;
        originalDebtorAgent.Element(ns + "Othr")!.Element(ns + "Id")!.Value.ShouldBe("SMNDA");
    }

    [Fact]
    public void Write_MandateAmendmentWithOriginalDebtorAgentBic_WritesVersionSpecificBicElement()
    {
        var initiation = MinimalInitiation();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalDebtorAgent = new FinancialInstitution { Bic = "MARKDEF1100" };

        var document = Pain008Writer.Write(initiation, Pain008Version.V02);

        var ns = document.Root!.Name.Namespace;
        var originalDebtorAgent = document.Descendants(ns + "OrgnlDbtrAgt").Single();
        originalDebtorAgent.Descendants(ns + "BIC").Single().Value.ShouldBe("MARKDEF1100");
    }

    [Theory]
    [InlineData(Pain008Version.V02, "pain.008.001.02.xsd")]
    [InlineData(Pain008Version.V08, "pain.008.001.08.xsd")]
    public void Write_MandateAmendmentWithOriginalDebtorAccountOtherId_WritesOthrIdAndValidatesAgainstSchema(Pain008Version version, string schemaFileName)
    {
        var initiation = MinimalInitiation();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalDebtorAccount = new AccountIdentification { OtherId = "SMNDA" };

        var xml = Pain008Writer.WriteToString(initiation, version);
        var document = XDocument.Parse(xml);

        var ns = document.Root!.Name.Namespace;
        var originalDebtorAccount = document.Descendants(ns + "OrgnlDbtrAcct").Single();
        originalDebtorAccount.Element(ns + "Id")!.Element(ns + "Othr")!.Element(ns + "Id")!.Value.ShouldBe("SMNDA");
        originalDebtorAccount.Descendants(ns + "IBAN").ShouldBeEmpty();

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(12.3, "12.30")]
    [InlineData(12.34, "12.34")]
    public void Write_Amount_FormatsWithExactlyTwoDecimalPlaces(decimal amount, string expected)
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(amount, "EUR");

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "InstdAmt").Single().Value.ShouldBe(expected);
    }

    [Fact]
    public void Write_MultiplePaymentInformationsAndTransactions_ComputesTotalsAtEachLevel()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions.Add(new DirectDebitTransaction
        {
            EndToEndId = "E2E-0002",
            Amount = new Money(50.00m, "EUR"),
            Mandate = new MandateInformation { MandateId = "MANDATE-0002", DateOfSignature = new DateOnly(2026, 1, 11) },
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
        });

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "GrpHdr").Single().Element(ns + "NbOfTxs")!.Value.ShouldBe("2");
        document.Descendants(ns + "GrpHdr").Single().Element(ns + "CtrlSum")!.Value.ShouldBe("150.00");
        document.Descendants(ns + "PmtInf").Single().Element(ns + "NbOfTxs")!.Value.ShouldBe("2");
        document.Descendants(ns + "PmtInf").Single().Element(ns + "CtrlSum")!.Value.ShouldBe("150.00");
    }

    [Fact]
    public void Write_RemittanceInformation_WritesUstrd()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = ["Membership fee September 2026"],
        };

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "RmtInf").Single().Element(ns + "Ustrd")!.Value.ShouldBe("Membership fee September 2026");
    }

    [Fact]
    public void Write_UltimateDebtorAndCreditor_WritesAtCorrectLevels()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].UltimateCreditor = new PartyIdentification { Name = "Ultimate Creditor Group" };
        initiation.PaymentInformations[0].Transactions[0].UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Sub" };

        var document = Pain008Writer.Write(initiation, Pain008Version.V08);

        var ns = document.Root!.Name.Namespace;
        document.Descendants(ns + "PmtInf").Single().Element(ns + "UltmtCdtr")!.Element(ns + "Nm")!.Value.ShouldBe("Ultimate Creditor Group");
        document.Descendants(ns + "DrctDbtTxInf").Single().Element(ns + "UltmtDbtr")!.Element(ns + "Nm")!.Value.ShouldBe("Ultimate Debtor Sub");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WriteToString_OmitXmlDeclaration_ControlsDeclarationPresence(bool omit)
    {
        var xml = Pain008Writer.WriteToString(MinimalInitiation(), Pain008Version.V08, new Pain00xWriterOptions { OmitXmlDeclaration = omit });

        xml.StartsWith("<?xml", StringComparison.Ordinal).ShouldBe(!omit);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_OmitXmlDeclaration_ControlsDocumentDeclaration(bool omit)
    {
        var document = Pain008Writer.Write(MinimalInitiation(), Pain008Version.V08, new Pain00xWriterOptions { OmitXmlDeclaration = omit });

        if (omit)
            document.Declaration.ShouldBeNull();
        else
            document.Declaration.ShouldNotBeNull();
    }

    [Fact]
    public void Write_UndefinedVersion_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Pain008Writer.Write(MinimalInitiation(), (Pain008Version)99));
    }

    [Theory]
    [InlineData(Pain008Version.V02, "pain.008.001.02")]
    [InlineData(Pain008Version.V08, "pain.008.001.08")]
    public void WriteToString_RoundtripsThroughIdentify(Pain008Version version, string expectedIdentifier)
    {
        var xml = Pain008Writer.WriteToString(MinimalInitiation(), version);

        var identifier = Iso20022Document.Identify(Iso20022Document.Load(xml));

        identifier.Type.ShouldBe(Iso20022MessageType.Pain008);
        identifier.Identifier.ShouldBe(expectedIdentifier);
    }

    [Fact]
    public void Write_NoPaymentInformations_ThrowsValidationException()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations.Clear();

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain008Writer.Write(initiation, Pain008Version.V08));

        exception.Path.ShouldBe("PaymentInformations");
    }

    [Fact]
    public void Write_MissingMandateDateOfSignature_ThrowsValidationException()
    {
        var initiation = MinimalInitiation();
        initiation.PaymentInformations[0].Transactions[0].Mandate.DateOfSignature = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => Pain008Writer.Write(initiation, Pain008Version.V08));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.DateOfSignature");
    }

    [Fact]
    public void Write_NullInitiation_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Pain008Writer.Write(null!, Pain008Version.V08));
    }
}
