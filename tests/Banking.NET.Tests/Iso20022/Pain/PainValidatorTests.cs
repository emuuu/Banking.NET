using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Iso20022.Pain;

public class PainValidatorTests
{
    private const string ValidIban1 = "DE89370400440532013000";
    private const string ValidIban2 = "DE02120300000000202051";
    private const string ValidIban3 = "DE75512108001245126199";
    private const string ManipulatedIban = "DE89370400440532013001";

    private static CreditTransferInitiation ValidCreditTransfer() => new()
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
                DebtorAccount = new AccountIdentification { Iban = ValidIban1 },
                Transactions =
                [
                    new CreditTransferTransaction
                    {
                        EndToEndId = "E2E-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                        CreditorAccount = new AccountIdentification { Iban = ValidIban2 },
                    },
                ],
            },
        ],
    };

    private static DirectDebitInitiation ValidDirectDebit() => new()
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
                CreditorAccount = new AccountIdentification { Iban = ValidIban2 },
                CreditorSchemeId = "DE98ZZZ09999999999",
                Transactions =
                [
                    new DirectDebitTransaction
                    {
                        EndToEndId = "E2E-0001",
                        Amount = new Money(100.00m, "EUR"),
                        Mandate = new MandateInformation { MandateId = "MANDATE-0001", DateOfSignature = new DateOnly(2026, 1, 10) },
                        Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                        DebtorAccount = new AccountIdentification { Iban = ValidIban1 },
                    },
                ],
            },
        ],
    };

    private static void ValidateCreditTransfer(CreditTransferInitiation initiation, Pain00xWriterOptions? options = null) =>
        PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V03, options ?? new Pain00xWriterOptions());

    private static void ValidateDirectDebit(DirectDebitInitiation initiation, Pain00xWriterOptions? options = null) =>
        PainValidator.ValidateDirectDebit(initiation, Pain008Version.V02, options ?? new Pain00xWriterOptions());

    [Fact]
    public void ValidateCreditTransfer_ValidMinimalInitiation_DoesNotThrow() =>
        Should.NotThrow(() => ValidateCreditTransfer(ValidCreditTransfer()));

    [Fact]
    public void ValidateCreditTransfer_MissingMessageId_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.MessageId = " ";

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("MessageId");
    }

    [Fact]
    public void ValidateCreditTransfer_MessageIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.MessageId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("MessageId");
    }

    [Fact]
    public void ValidateCreditTransfer_MessageIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.MessageId = new string('A', 35);

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_MissingInitiatingPartyName_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("InitiatingParty.Name");
    }

    [Fact]
    public void ValidateCreditTransfer_InitiatingPartyNameExceeds70CharactersInV03_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = new string('A', 71);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("InitiatingParty.Name");
    }

    [Fact]
    public void ValidateCreditTransfer_InitiatingPartyNameAt140CharactersInV09_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = new string('A', 140);

        Should.NotThrow(() => PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V09, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateCreditTransfer_NoPaymentInformations_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations.Clear();

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations");
    }

    [Fact]
    public void ValidateCreditTransfer_MissingPaymentInformationId_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].PaymentInformationId = string.Empty;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].PaymentInformationId");
    }

    [Fact]
    public void ValidateCreditTransfer_PaymentInformationIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].PaymentInformationId = new string('A', 35);

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_PaymentInformationIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].PaymentInformationId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].PaymentInformationId");
    }

    [Fact]
    public void ValidateCreditTransfer_NoTransactions_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions.Clear();

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions");
    }

    [Fact]
    public void ValidateCreditTransfer_InstructionIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].InstructionId = new string('A', 35);

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_InstructionIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].InstructionId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].InstructionId");
    }

    [Fact]
    public void ValidateCreditTransfer_InstructionIdEmpty_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].InstructionId = string.Empty;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].InstructionId");
    }

    [Fact]
    public void ValidateCreditTransfer_UltimateDebtorNameWhitespace_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].UltimateDebtor = new PartyIdentification { Name = " " };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].UltimateDebtor.Name");
    }

    [Fact]
    public void ValidateCreditTransfer_MissingEndToEndId_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].EndToEndId = string.Empty;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].EndToEndId");
    }

    [Fact]
    public void ValidateCreditTransfer_EndToEndIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].EndToEndId = new string('A', 35);

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_EndToEndIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].EndToEndId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].EndToEndId");
    }

    [Fact]
    public void ValidateCreditTransfer_UndefinedInstructionPriority_ThrowsArgumentOutOfRangeException()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].InstructionPriority = (InstructionPriority)99;

        Should.Throw<ArgumentOutOfRangeException>(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_UndefinedChargeBearer_ThrowsArgumentOutOfRangeException()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].ChargeBearer = (ChargeBearer)99;

        Should.Throw<ArgumentOutOfRangeException>(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CharacterSetValidationEnabled_OrganisationIssuerInvalidCharacters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Debtor.OrganisationId = new OrganisationIdentification
        {
            OtherId = "HRB12345",
            OtherIssuer = "Handelsregister & Co",
        };

        var exception = Should.Throw<Iso20022ValidationException>(
            () => ValidateCreditTransfer(initiation, new Pain00xWriterOptions { ValidateCharacterSet = true }));

        exception.Path.ShouldBe("PaymentInformations[0].Debtor.OrganisationId.OtherIssuer");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateCreditTransfer_AmountNotGreaterThanZero_ThrowsWithPath(decimal amount)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(amount, "EUR");

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Amount");
    }

    [Fact]
    public void ValidateCreditTransfer_AmountWithMoreThanTwoDecimalPlaces_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(12.345m, "EUR");

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Amount");
    }

    [Fact]
    public void ValidateCreditTransfer_AmountWithTrailingZeroDecimals_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(12.340m, "EUR");

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("12R")]
    [InlineData("eur")]
    public void ValidateCreditTransfer_InvalidCurrencyCode_ThrowsWithPath(string currency)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(100.00m, currency);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Amount.Currency");
    }

    [Fact]
    public void ValidateCreditTransfer_AmountCurrencyWithTrailingNewline_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(100.00m, "EUR\n");

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Amount.Currency");
    }

    [Fact]
    public void ValidateCreditTransfer_AmountAt18TotalDigits_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(1234567890123456.78m, "EUR");

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_AmountExceeds18TotalDigits_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(12345678901234567.89m, "EUR");

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Amount");
    }

    [Fact]
    public void ValidateCreditTransfer_MissingCreditorName_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Creditor.Name = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Creditor.Name");
    }

    [Fact]
    public void ValidateCreditTransfer_UltimateDebtorNameExceedsMaxLength_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].UltimateDebtor = new PartyIdentification { Name = new string('A', 71) };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].UltimateDebtor.Name");
    }

    [Fact]
    public void ValidateCreditTransfer_MissingDebtorIban_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountWithOnlyOtherId_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { OtherId = "OTHER-ACCT-0001" };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountWithBothIbanAndOtherId_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { Iban = ValidIban1, OtherId = "OTHER-ACCT-0001" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountWithIbanAndWhitespaceOnlyOtherId_ThrowsWithOtherIdPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { Iban = ValidIban1, OtherId = "   " };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.OtherId");
    }

    [Fact]
    public void ValidateCreditTransfer_IbanAsDigitsOnly_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = "000000000000054";

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.Iban");
    }

    [Fact]
    public void ValidateCreditTransfer_IbanWithManipulatedCheckDigits_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = ManipulatedIban;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.Iban");
        exception.Message.ShouldContain("check digits");
    }

    [Fact]
    public void ValidateCreditTransfer_IbanWithWhitespaceAndLowerCase_NormalizesAndDoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = "de89 3704 0044 0532 0130 00";

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Theory]
    [InlineData(ValidIban1)]
    [InlineData(ValidIban2)]
    [InlineData(ValidIban3)]
    public void ValidateCreditTransfer_ValidIbanCheckDigits_DoesNotThrow(string iban)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = iban;

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Theory]
    [InlineData("DE1234")]
    [InlineData("DE897CHFTOOLONGXXXXXXXXXXXXXXXXXXXXXXXXX")]
    public void ValidateCreditTransfer_IbanWithInvalidLength_ThrowsWithPath(string iban)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Iban = iban;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.Iban");
    }

    [Theory]
    [InlineData("COBADEFF")]
    [InlineData("COBADEFFXXX")]
    [InlineData("MARKDEF1100")]
    public void ValidateCreditTransfer_ValidBicsAtLegacyVersion_DoesNotThrow(string bic)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = bic };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_BicOnlyValidUnderCurrentPatternAtLegacyVersion_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "COBADE10" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_BicOnlyValidUnderCurrentPatternAtCurrentVersion_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "COBADE10" };

        Should.NotThrow(() => PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V09, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateCreditTransfer_BicWithTrailingNewlineAtLegacyVersion_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "COBADEFF\n" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_BicWithTrailingNewlineAtCurrentVersion_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "COBADEFF\n" };

        var exception = Should.Throw<Iso20022ValidationException>(() => PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V09, new Pain00xWriterOptions()));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Theory]
    [InlineData("COBA")]
    [InlineData("COBADEFFXXXXX")]
    public void ValidateCreditTransfer_BicWithInvalidLength_ThrowsWithPath(string bic)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = bic };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_BicNotMatchingPattern_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "!!!!!!!!" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_OrganisationBicInvalid_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Debtor.OrganisationId = new OrganisationIdentification { Bic = "!!!!!!!!" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Debtor.OrganisationId.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_UnstructuredAndStructuredRemittanceBothSet_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = ["Invoice 1"],
            CreditorReference = "RF18539007547034",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation");
    }

    [Fact]
    public void ValidateCreditTransfer_MoreThanOneUnstructuredRemittanceLine_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = ["Invoice 1", "Invoice 2"],
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.Unstructured");
    }

    [Fact]
    public void ValidateCreditTransfer_UnstructuredRemittanceLineExceeds140Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = [new string('A', 141)],
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.Unstructured[0]");
    }

    [Fact]
    public void ValidateCreditTransfer_SingleUnstructuredRemittanceLineAt140Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = [new string('A', 140)],
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_NonSepaServiceLevelWithMultipleUnstructuredLines_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].ServiceLevelCode = "URGP";
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            Unstructured = ["Invoice 1", "Invoice 2"],
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceAt35Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = new string('1', 35),
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = new string('1', 36),
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReference");
    }

    [Fact]
    public void ValidateCreditTransfer_CharacterSetValidationDisabledByDefault_DoesNotThrowForNonSepaCharacters()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = "Debtor & Co";

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CharacterSetValidationEnabled_ThrowsForNonSepaCharacters()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = "Debtor & Co";

        var exception = Should.Throw<Iso20022ValidationException>(
            () => ValidateCreditTransfer(initiation, new Pain00xWriterOptions { ValidateCharacterSet = true }));

        exception.Path.ShouldBe("InitiatingParty.Name");
        exception.Message.ShouldContain("SEPA character set");
    }

    [Fact]
    public void ValidateCreditTransfer_CharacterSetValidationEnabled_DoesNotThrowForSepaCharacters()
    {
        var initiation = ValidCreditTransfer();
        initiation.InitiatingParty.Name = "Example Debtor GmbH (Frankfurt)";

        Should.NotThrow(() => ValidateCreditTransfer(initiation, new Pain00xWriterOptions { ValidateCharacterSet = true }));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceWhitespaceOnly_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = " ",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReference");
    }

    [Theory]
    [InlineData("RADM")]
    [InlineData("RPIN")]
    [InlineData("FXDR")]
    [InlineData("DISP")]
    [InlineData("PUOR")]
    [InlineData("SCOR")]
    public void ValidateCreditTransfer_CreditorReferenceTypeCodeDefined_DoesNotThrow(string code)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = "RF18539007547034",
            CreditorReferenceTypeCode = code,
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeCodeUndefined_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReference = "RF18539007547034",
            CreditorReferenceTypeCode = "XXXX",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceTypeCode");
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeCodeWithoutReference_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeCode = "SCOR",
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeCodeUndefinedWithoutReference_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeCode = "XXXX",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceTypeCode");
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeCodeAndProprietaryBothSet_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeCode = "SCOR",
            CreditorReferenceTypeProprietary = "CUST",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceTypeProprietary");
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeProprietaryWhitespaceOnly_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeProprietary = " ",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceTypeProprietary");
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceTypeProprietaryWithoutReference_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceTypeProprietary = "CUST",
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceIssuerWithoutReference_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceIssuer = "Example Issuer",
        };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceIssuerExceeds35CharactersWithoutReference_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceIssuer = new string('A', 36),
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceIssuer");
    }

    [Fact]
    public void ValidateCreditTransfer_CreditorReferenceIssuerWhitespaceOnlyWithoutReference_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].RemittanceInformation = new RemittanceInformation
        {
            CreditorReferenceIssuer = " ",
        };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].RemittanceInformation.CreditorReferenceIssuer");
    }

    [Fact]
    public void ValidateCreditTransfer_ServiceLevelCodeEmpty_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].ServiceLevelCode = string.Empty;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].ServiceLevelCode");
    }

    [Fact]
    public void ValidateCreditTransfer_ServiceLevelCodeValid_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].ServiceLevelCode = "URGP";

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_PurposeCodeExceeds4Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].PurposeCode = "ABCDE";

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].PurposeCode");
    }

    [Fact]
    public void ValidateCreditTransfer_PurposeCodeAt4Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].PurposeCode = "SUPP";

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountOtherIdAt34Characters_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { OtherId = new string('A', 34) };

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountOtherIdExceeds34Characters_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { OtherId = new string('A', 35) };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.OtherId");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountOtherIdWhitespaceOnly_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount = new AccountIdentification { OtherId = " " };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.OtherId");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountCurrencyLowercase_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Currency = "eur";

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.Currency");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountCurrencyWithTrailingNewline_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Currency = "EUR\n";

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAccount.Currency");
    }

    [Fact]
    public void ValidateCreditTransfer_DebtorAccountCurrencyValid_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAccount.Currency = "EUR";

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_BicWithDigitInFirstSixPositionsAtLegacyVersion_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "C0BADEFF" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].DebtorAgent.Bic");
    }

    [Fact]
    public void ValidateCreditTransfer_BicWithDigitInFirstSixPositionsAtCurrentVersion_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = "C0BADEFF" };

        Should.NotThrow(() => PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V09, new Pain00xWriterOptions()));
    }

    [Theory]
    [InlineData("COBADEFF")]
    [InlineData("COBADEFFXXX")]
    [InlineData("MARKDEF1100")]
    public void ValidateCreditTransfer_ValidBicsAtCurrentVersion_DoesNotThrow(string bic)
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].DebtorAgent = new FinancialInstitution { Bic = bic };

        Should.NotThrow(() => PainValidator.ValidateCreditTransfer(initiation, Pain001Version.V09, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateCreditTransfer_PaymentInformationControlSumExceeds18TotalDigits_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        var pmtInf = initiation.PaymentInformations[0];
        pmtInf.Transactions[0].Amount = new Money(9999999999999999.99m, "EUR");
        pmtInf.Transactions.Add(new CreditTransferTransaction
        {
            EndToEndId = "E2E-0002",
            Amount = new Money(9999999999999999.99m, "EUR"),
            Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
            CreditorAccount = new AccountIdentification { Iban = ValidIban2 },
        });

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].ControlSum");
    }

    [Fact]
    public void ValidateCreditTransfer_PaymentInformationControlSumAt18TotalDigits_DoesNotThrow()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(9999999999999999.99m, "EUR");

        Should.NotThrow(() => ValidateCreditTransfer(initiation));
    }

    [Fact]
    public void ValidateCreditTransfer_TotalControlSumExceeds18TotalDigits_ThrowsWithPath()
    {
        var initiation = ValidCreditTransfer();
        initiation.PaymentInformations[0].Transactions[0].Amount = new Money(9999999999999999.99m, "EUR");
        initiation.PaymentInformations.Add(new CreditTransferPaymentInformation
        {
            PaymentInformationId = "PMTINF-0002",
            RequestedExecutionDate = new DateOnly(2026, 9, 16),
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = ValidIban1 },
            Transactions =
            [
                new CreditTransferTransaction
                {
                    EndToEndId = "E2E-0003",
                    Amount = new Money(9999999999999999.99m, "EUR"),
                    Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                    CreditorAccount = new AccountIdentification { Iban = ValidIban2 },
                },
            ],
        });

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateCreditTransfer(initiation));

        exception.Path.ShouldBe("ControlSum");
    }

    [Fact]
    public void ValidateDirectDebit_ValidMinimalInitiation_DoesNotThrow() =>
        Should.NotThrow(() => ValidateDirectDebit(ValidDirectDebit()));

    [Fact]
    public void ValidateDirectDebit_MissingCreditorSchemeId_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorSchemeId = string.Empty;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].CreditorSchemeId");
    }

    [Fact]
    public void ValidateDirectDebit_CreditorSchemeIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorSchemeId = new string('A', 35);

        Should.NotThrow(() => ValidateDirectDebit(initiation));
    }

    [Fact]
    public void ValidateDirectDebit_CreditorSchemeIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorSchemeId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].CreditorSchemeId");
    }

    [Fact]
    public void ValidateDirectDebit_UndefinedScheme_ThrowsArgumentOutOfRangeException()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Scheme = (DirectDebitScheme)99;

        Should.Throw<ArgumentOutOfRangeException>(() => ValidateDirectDebit(initiation));
    }

    [Fact]
    public void ValidateDirectDebit_UndefinedSequenceType_ThrowsArgumentOutOfRangeException()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].SequenceType = (SequenceType)99;

        Should.Throw<ArgumentOutOfRangeException>(() => ValidateDirectDebit(initiation));
    }

    [Fact]
    public void ValidateDirectDebit_CreditorNameExceeds70CharactersInV02_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Creditor.Name = new string('A', 71);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Creditor.Name");
    }

    [Fact]
    public void ValidateDirectDebit_CreditorNameAt70CharactersInV02_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Creditor.Name = new string('A', 70);

        Should.NotThrow(() => ValidateDirectDebit(initiation));
    }

    [Fact]
    public void ValidateDirectDebit_CreditorNameAt140CharactersInV08_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Creditor.Name = new string('A', 140);

        Should.NotThrow(() => PainValidator.ValidateDirectDebit(initiation, Pain008Version.V08, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateDirectDebit_CreditorNameExceeds140CharactersInV08_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Creditor.Name = new string('A', 141);

        var exception = Should.Throw<Iso20022ValidationException>(() => PainValidator.ValidateDirectDebit(initiation, Pain008Version.V08, new Pain00xWriterOptions()));

        exception.Path.ShouldBe("PaymentInformations[0].Creditor.Name");
    }

    [Fact]
    public void ValidateDirectDebit_BicWithDigitInFirstSixPositionsAtLegacyVersion_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = "C0BADEFF" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].CreditorAgent.Bic");
    }

    [Fact]
    public void ValidateDirectDebit_BicWithDigitInFirstSixPositionsAtCurrentVersion_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = "C0BADEFF" };

        Should.NotThrow(() => PainValidator.ValidateDirectDebit(initiation, Pain008Version.V08, new Pain00xWriterOptions()));
    }

    [Theory]
    [InlineData("COBADEFF")]
    [InlineData("COBADEFFXXX")]
    [InlineData("MARKDEF1100")]
    public void ValidateDirectDebit_ValidBicsAtLegacyVersion_DoesNotThrow(string bic)
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = bic };

        Should.NotThrow(() => ValidateDirectDebit(initiation));
    }

    [Theory]
    [InlineData("COBADEFF")]
    [InlineData("COBADEFFXXX")]
    [InlineData("MARKDEF1100")]
    public void ValidateDirectDebit_ValidBicsAtCurrentVersion_DoesNotThrow(string bic)
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = bic };

        Should.NotThrow(() => PainValidator.ValidateDirectDebit(initiation, Pain008Version.V08, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateDirectDebit_BicOnlyValidUnderCurrentPatternAtLegacyVersion_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = "COBADE10" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].CreditorAgent.Bic");
    }

    [Fact]
    public void ValidateDirectDebit_BicOnlyValidUnderCurrentPatternAtCurrentVersion_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAgent = new FinancialInstitution { Bic = "COBADE10" };

        Should.NotThrow(() => PainValidator.ValidateDirectDebit(initiation, Pain008Version.V08, new Pain00xWriterOptions()));
    }

    [Fact]
    public void ValidateDirectDebit_MissingMandateId_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Transactions[0].Mandate.MandateId = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.MandateId");
    }

    [Fact]
    public void ValidateDirectDebit_MandateIdAt35Characters_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Transactions[0].Mandate.MandateId = new string('A', 35);

        Should.NotThrow(() => ValidateDirectDebit(initiation));
    }

    [Fact]
    public void ValidateDirectDebit_MandateIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Transactions[0].Mandate.MandateId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.MandateId");
    }

    [Fact]
    public void ValidateDirectDebit_MissingMandateDateOfSignature_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Transactions[0].Mandate.DateOfSignature = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.DateOfSignature");
    }

    [Fact]
    public void ValidateDirectDebit_MissingDebtorIban_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].Transactions[0].DebtorAccount.Iban = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].DebtorAccount");
    }

    [Fact]
    public void ValidateDirectDebit_MissingCreditorAccountIban_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        initiation.PaymentInformations[0].CreditorAccount.Iban = null;

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].CreditorAccount");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalMandateIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalMandateId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalMandateId");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalCreditorSchemeIdExceeds35Characters_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalCreditorSchemeId = new string('A', 36);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalCreditorSchemeId");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalCreditorNameExceedsMaxLengthInV02_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalCreditorName = new string('A', 71);

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalCreditorName");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalDebtorAccountIbanAsDigitsOnly_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalDebtorAccount = new AccountIdentification { Iban = "000000000000054" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalDebtorAccount.Iban");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalDebtorAgentInvalidBic_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalDebtorAgent = new FinancialInstitution { Bic = "!!!!!!!!" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalDebtorAgent.Bic");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentOriginalDebtorAccountWithBothIbanAndOtherId_ThrowsWithPath()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.AmendmentIndicator = true;
        mandate.OriginalDebtorAccount = new AccountIdentification { Iban = ValidIban1, OtherId = "OTHER-ACCT-0001" };

        var exception = Should.Throw<Iso20022ValidationException>(() => ValidateDirectDebit(initiation));

        exception.Path.ShouldBe("PaymentInformations[0].Transactions[0].Mandate.OriginalDebtorAccount");
    }

    [Fact]
    public void ValidateDirectDebit_MandateAmendmentFieldsWithoutIndicator_DoesNotThrow()
    {
        var initiation = ValidDirectDebit();
        var mandate = initiation.PaymentInformations[0].Transactions[0].Mandate;
        mandate.OriginalMandateId = new string('A', 36);

        Should.NotThrow(() => ValidateDirectDebit(initiation));
    }
}
