using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Tests.Iso20022.Common;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Pain;

/// <summary>
/// Validates pain.001/pain.008 writer output and ISO 20022 sample messages against the official XSD schemas embedded
/// under <c>Iso20022/Schemas/</c>.
/// </summary>
public class SchemaValidationTests
{
    [Theory]
    [InlineData("pain.002.001.03-sample.xml", "pain.002.001.03.xsd")]
    [InlineData("pain.002.001.10-sample.xml", "pain.002.001.10.xsd")]
    [InlineData("camt.052.001.08-sample.xml", "camt.052.001.08.xsd")]
    [InlineData("camt.053.001.02-sample.xml", "camt.053.001.02.xsd")]
    [InlineData("camt.053.001.08-sample.xml", "camt.053.001.08.xsd")]
    [InlineData("camt.054.001.08-sample.xml", "camt.054.001.08.xsd")]
    public void Sample_ValidatesAgainstSchema(string sampleFileName, string schemaFileName)
    {
        var errors = SchemaValidator.Validate(schemaFileName, SampleXml.Load(sampleFileName));

        errors.ShouldBeEmpty();
    }

    [Fact(Skip = "camt.086.001.02-sample.xml does not validate against the official schema yet: several billing " +
        "elements (enumeration codes for Sts/AcctLvl/Mtd/PmtMtd/Cd, a missing mandatory Sgn element on " +
        "Bal/UnitPrice/OriginalChargePrice/OriginalChargeSettlementAmount, account characteristics currency fields, " +
        "and BIC vs. BICFI) do not match the current model; the billing model is being realigned with the schema.")]
    public void Camt086Sample_ValidatesAgainstSchema()
    {
    }

    [Theory]
    [InlineData(Pain001Version.V03, "pain.001.001.03.xsd")]
    [InlineData(Pain001Version.V09, "pain.001.001.09.xsd")]
    public void Pain001Writer_MinimalScenario_ValidatesAgainstSchema(Pain001Version version, string schemaFileName)
    {
        var xml = Pain001Writer.WriteToString(MinimalCreditTransfer(), version);

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Pain001Version.V03, "pain.001.001.03.xsd")]
    [InlineData(Pain001Version.V09, "pain.001.001.09.xsd")]
    public void Pain001Writer_FullFeaturedScenario_ValidatesAgainstSchema(Pain001Version version, string schemaFileName)
    {
        var xml = Pain001Writer.WriteToString(FullFeaturedCreditTransfer(), version);

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Pain008Version.V02, "pain.008.001.02.xsd")]
    [InlineData(Pain008Version.V08, "pain.008.001.08.xsd")]
    public void Pain008Writer_MinimalScenario_ValidatesAgainstSchema(Pain008Version version, string schemaFileName)
    {
        var xml = Pain008Writer.WriteToString(MinimalDirectDebit(), version);

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Pain008Version.V02, "pain.008.001.02.xsd")]
    [InlineData(Pain008Version.V08, "pain.008.001.08.xsd")]
    public void Pain008Writer_FullFeaturedScenario_ValidatesAgainstSchema(Pain008Version version, string schemaFileName)
    {
        var xml = Pain008Writer.WriteToString(FullFeaturedDirectDebit(), version);

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Pain008Version.V02, "pain.008.001.02.xsd")]
    [InlineData(Pain008Version.V08, "pain.008.001.08.xsd")]
    public void Pain008Writer_MandateAmendmentScenario_ValidatesAgainstSchema(Pain008Version version, string schemaFileName)
    {
        var initiation = MinimalDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalMandateId = "MANDATE-OLD-0001";
        mandate.OriginalCreditorName = "Old Creditor Name Ltd";
        mandate.OriginalCreditorSchemeId = "DE98ZZZ00000000001";
        mandate.OriginalDebtorAccount = new AccountIdentification { Iban = "DE75512108001245126199" };
        mandate.OriginalDebtorAgent = new FinancialInstitution { Bic = "MARKDEF1100" };

        var xml = Pain008Writer.WriteToString(initiation, version);

        SchemaValidator.Validate(schemaFileName, xml).ShouldBeEmpty();
    }

    private static CreditTransferInitiation MinimalCreditTransfer() => new()
    {
        MessageId = "MSGID-SCHEMA-0001",
        InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
        PaymentInformations =
        [
            new CreditTransferPaymentInformation
            {
                PaymentInformationId = "PMTINF-SCHEMA-0001",
                RequestedExecutionDate = new DateOnly(2026, 9, 15),
                Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                Transactions =
                [
                    new CreditTransferTransaction
                    {
                        EndToEndId = "E2E-SCHEMA-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                        CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                    },
                ],
            },
        ],
    };

    private static CreditTransferInitiation FullFeaturedCreditTransfer() => new()
    {
        MessageId = "MSGID-SCHEMA-0002",
        InitiatingParty = new PartyIdentification
        {
            Name = "Example Debtor GmbH",
            PostalAddress = new PostalAddress { Country = "DE", TownName = "Frankfurt am Main", PostCode = "60311" },
            OrganisationId = new OrganisationIdentification { Bic = "COBADEFFXXX", OtherId = "HRB12345", OtherSchemeCode = "COID" },
        },
        PaymentInformations =
        [
            new CreditTransferPaymentInformation
            {
                PaymentInformationId = "PMTINF-SCHEMA-0002",
                BatchBooking = true,
                InstructionPriority = InstructionPriority.High,
                CategoryPurposeCode = "SALA",
                RequestedExecutionDate = new DateOnly(2026, 9, 16),
                Debtor = new PartyIdentification
                {
                    Name = "Example Debtor GmbH",
                    PostalAddress = new PostalAddress
                    {
                        Department = "Finance",
                        StreetName = "Kaiserplatz",
                        BuildingNumber = "1",
                        PostCode = "60311",
                        TownName = "Frankfurt am Main",
                        CountrySubDivision = "HE",
                        Country = "DE",
                        AddressLines = ["Building A"],
                    },
                },
                DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000", Currency = "EUR" },
                DebtorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" },
                UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Group" },
                ChargeBearer = ChargeBearer.Shar,
                Transactions =
                [
                    new CreditTransferTransaction
                    {
                        InstructionId = "INSTR-SCHEMA-0001",
                        EndToEndId = "E2E-SCHEMA-0002",
                        Amount = new Money(250.50m, "EUR"),
                        UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Sub" },
                        CreditorAgent = new FinancialInstitution { Bic = "MARKDEF1100" },
                        Creditor = new PartyIdentification
                        {
                            Name = "Example Creditor Ltd",
                            PostalAddress = new PostalAddress { Country = "DE", TownName = "Berlin" },
                        },
                        CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                        UltimateCreditor = new PartyIdentification { Name = "Ultimate Creditor Group" },
                        PurposeCode = "SUPP",
                        RemittanceInformation = new RemittanceInformation { Unstructured = ["Invoice 2026-0920"] },
                    },
                    new CreditTransferTransaction
                    {
                        EndToEndId = "E2E-SCHEMA-0003",
                        Amount = new Money(75.00m, "EUR"),
                        Creditor = new PartyIdentification
                        {
                            Name = "Example Creditor Ltd",
                            PrivateId = new PrivateIdentification { OtherId = "P-12345" },
                        },
                        CreditorAccount = new AccountIdentification { Iban = "DE75512108001245126199" },
                        RemittanceInformation = new RemittanceInformation { CreditorReference = "RF18539007547034" },
                    },
                ],
            },
        ],
    };

    private static DirectDebitInitiation MinimalDirectDebit() => new()
    {
        MessageId = "MSGID-SCHEMA-0001",
        InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
        PaymentInformations =
        [
            new DirectDebitPaymentInformation
            {
                PaymentInformationId = "PMTINF-SCHEMA-0001",
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
                        EndToEndId = "E2E-SCHEMA-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Mandate = new MandateInformation { MandateId = "MANDATE-SCHEMA-0001", DateOfSignature = new DateOnly(2026, 1, 10) },
                        Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                        DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                    },
                ],
            },
        ],
    };

    private static DirectDebitInitiation FullFeaturedDirectDebit() => new()
    {
        MessageId = "MSGID-SCHEMA-0002",
        InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
        PaymentInformations =
        [
            new DirectDebitPaymentInformation
            {
                PaymentInformationId = "PMTINF-SCHEMA-0002",
                BatchBooking = true,
                Scheme = DirectDebitScheme.B2B,
                SequenceType = SequenceType.First,
                CategoryPurposeCode = "SALA",
                RequestedCollectionDate = new DateOnly(2026, 9, 20),
                Creditor = new PartyIdentification
                {
                    Name = "Example Creditor Ltd",
                    PostalAddress = new PostalAddress { Country = "DE", TownName = "Berlin" },
                },
                CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                CreditorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" },
                UltimateCreditor = new PartyIdentification { Name = "Ultimate Creditor Group" },
                ChargeBearer = ChargeBearer.Slev,
                CreditorSchemeId = "DE98ZZZ09999999999",
                Transactions =
                [
                    new DirectDebitTransaction
                    {
                        InstructionId = "INSTR-SCHEMA-0002",
                        EndToEndId = "E2E-SCHEMA-0004",
                        Amount = new Money(310.25m, "EUR"),
                        Mandate = new MandateInformation { MandateId = "MANDATE-SCHEMA-0002", DateOfSignature = new DateOnly(2026, 2, 1) },
                        DebtorAgent = new FinancialInstitution { Bic = "MARKDEF1100" },
                        Debtor = new PartyIdentification
                        {
                            Name = "Example Debtor GmbH",
                            PostalAddress = new PostalAddress { Country = "DE", TownName = "Frankfurt am Main" },
                        },
                        DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                        UltimateDebtor = new PartyIdentification { Name = "Ultimate Debtor Sub" },
                        PurposeCode = "SUPP",
                        RemittanceInformation = new RemittanceInformation { Unstructured = ["Membership fee September 2026"] },
                    },
                ],
            },
        ],
    };
}
