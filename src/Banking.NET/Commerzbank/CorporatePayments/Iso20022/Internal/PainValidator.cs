using System.Globalization;
using System.Text.RegularExpressions;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

/// <summary>Validates pain.001/pain.008 writer models before <see cref="Pain001Writer"/>/<see cref="Pain008Writer"/> render them to XML.</summary>
/// <remarks>Failure paths use C# property navigation, e.g. <c>PaymentInformations[0].Transactions[2].EndToEndId</c>, not XML element paths.</remarks>
internal static class PainValidator
{
    private const int IdMaxLength = 35;
    private const int RemittanceLineMaxLength = 140;
    private const int AmountMaxTotalDigits = 18;
    private const int OtherIdMaxLength = 34;
    private const int ServiceLevelCodeMaxLength = 4;
    private const int LocalInstrumentCodeMaxLength = 35;
    private const int CategoryPurposeCodeMaxLength = 4;
    private const int PurposeCodeMaxLength = 4;

    /// <remarks>Anchored with <c>\z</c> rather than <c>$</c>, since <c>$</c> also matches immediately before a trailing <c>\n</c> in .NET regex.</remarks>
    private static readonly Regex IbanFormat = new(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The BIC pattern of the current schemas (<c>BICFIDec2014Identifier</c>/<c>AnyBICDec2014Identifier</c>).</summary>
    /// <remarks>Anchored with <c>\z</c> rather than <c>$</c>, since <c>$</c> also matches immediately before a trailing <c>\n</c> in .NET regex.</remarks>
    private static readonly Regex BicFormatCurrent = new(@"^[A-Z0-9]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The BIC pattern of the legacy schemas (<c>BICIdentifier</c>/<c>AnyBICIdentifier</c>): no digits in positions 1-6.</summary>
    /// <remarks>Anchored with <c>\z</c> rather than <c>$</c>, since <c>$</c> also matches immediately before a trailing <c>\n</c> in .NET regex.</remarks>
    private static readonly Regex BicFormatLegacy = new(@"^[A-Z]{6}[A-Z2-9][A-NP-Z0-9]([A-Z0-9]{3})?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <remarks>Anchored with <c>\z</c> rather than <c>$</c>, since <c>$</c> also matches immediately before a trailing <c>\n</c> in .NET regex.</remarks>
    private static readonly Regex CurrencyFormat = new(@"^[A-Z]{3}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The allowed creditor reference type codes (ISO 20022 <c>DocumentType3Code</c>), identical across all four pain.001/pain.008 schema versions.</summary>
    private static readonly string[] CreditorReferenceTypeCodes = ["RADM", "RPIN", "FXDR", "DISP", "PUOR", "SCOR"];

    /// <summary>Validates a pain.001 credit transfer initiation.</summary>
    /// <exception cref="Iso20022ValidationException">The initiation fails a validation rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    public static void ValidateCreditTransfer(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions options)
    {
        var isCurrentVersion = PainWriterHelpers.IsCurrentVersion(version);
        var nameMaxLength = PainWriterHelpers.MaxNameLength(version);

        ValidateId(initiation.MessageId, "MessageId", options);
        ValidateName(initiation.InitiatingParty.Name, nameMaxLength, "InitiatingParty.Name", options);
        ValidateParty(initiation.InitiatingParty, "InitiatingParty", options, isCurrentVersion);

        if (initiation.PaymentInformations.Count == 0)
            throw new Iso20022ValidationException("At least one payment information block is required.", "PaymentInformations");

        for (var i = 0; i < initiation.PaymentInformations.Count; i++)
        {
            var pmtInf = initiation.PaymentInformations[i];
            var pmtInfPath = $"PaymentInformations[{i}]";

            ValidateId(pmtInf.PaymentInformationId, $"{pmtInfPath}.PaymentInformationId", options);
            if (pmtInf.InstructionPriority is { } instructionPriority)
                ValidateEnumDefined(instructionPriority, $"{pmtInfPath}.InstructionPriority");
            if (pmtInf.ChargeBearer is { } pmtInfChargeBearer)
                ValidateEnumDefined(pmtInfChargeBearer, $"{pmtInfPath}.ChargeBearer");
            ValidateOptionalId(pmtInf.ServiceLevelCode, $"{pmtInfPath}.ServiceLevelCode", options, ServiceLevelCodeMaxLength);
            ValidateOptionalId(pmtInf.LocalInstrumentCode, $"{pmtInfPath}.LocalInstrumentCode", options, LocalInstrumentCodeMaxLength);
            ValidateOptionalId(pmtInf.CategoryPurposeCode, $"{pmtInfPath}.CategoryPurposeCode", options, CategoryPurposeCodeMaxLength);
            ValidateName(pmtInf.Debtor.Name, nameMaxLength, $"{pmtInfPath}.Debtor.Name", options);
            ValidateParty(pmtInf.Debtor, $"{pmtInfPath}.Debtor", options, isCurrentVersion);
            ValidateOptionalName(pmtInf.UltimateDebtor?.Name, nameMaxLength, $"{pmtInfPath}.UltimateDebtor.Name", options);
            ValidateParty(pmtInf.UltimateDebtor, $"{pmtInfPath}.UltimateDebtor", options, isCurrentVersion);
            ValidateAccount(pmtInf.DebtorAccount, $"{pmtInfPath}.DebtorAccount", options);
            ValidateOptionalBic(pmtInf.DebtorAgent?.Bic, $"{pmtInfPath}.DebtorAgent.Bic", isCurrentVersion);

            if (pmtInf.Transactions.Count == 0)
                throw new Iso20022ValidationException("A payment information block requires at least one transaction.", $"{pmtInfPath}.Transactions");

            for (var j = 0; j < pmtInf.Transactions.Count; j++)
            {
                var transaction = pmtInf.Transactions[j];
                var txPath = $"{pmtInfPath}.Transactions[{j}]";

                ValidateOptionalId(transaction.InstructionId, $"{txPath}.InstructionId", options);
                ValidateId(transaction.EndToEndId, $"{txPath}.EndToEndId", options);
                ValidateAmount(transaction.Amount, $"{txPath}.Amount");
                ValidateName(transaction.Creditor.Name, nameMaxLength, $"{txPath}.Creditor.Name", options);
                ValidateParty(transaction.Creditor, $"{txPath}.Creditor", options, isCurrentVersion);
                ValidateOptionalName(transaction.UltimateDebtor?.Name, nameMaxLength, $"{txPath}.UltimateDebtor.Name", options);
                ValidateParty(transaction.UltimateDebtor, $"{txPath}.UltimateDebtor", options, isCurrentVersion);
                ValidateOptionalName(transaction.UltimateCreditor?.Name, nameMaxLength, $"{txPath}.UltimateCreditor.Name", options);
                ValidateParty(transaction.UltimateCreditor, $"{txPath}.UltimateCreditor", options, isCurrentVersion);
                ValidateAccount(transaction.CreditorAccount, $"{txPath}.CreditorAccount", options);
                ValidateOptionalBic(transaction.CreditorAgent?.Bic, $"{txPath}.CreditorAgent.Bic", isCurrentVersion);
                ValidateOptionalId(transaction.PurposeCode, $"{txPath}.PurposeCode", options, PurposeCodeMaxLength);
                ValidateRemittance(transaction.RemittanceInformation, pmtInf.ServiceLevelCode, $"{txPath}.RemittanceInformation", options);
            }

            ValidateControlSum(pmtInf.ControlSum, $"{pmtInfPath}.ControlSum");
        }

        ValidateControlSum(initiation.ControlSum, "ControlSum");
    }

    /// <summary>Validates a pain.008 direct debit initiation.</summary>
    /// <exception cref="Iso20022ValidationException">The initiation fails a validation rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    public static void ValidateDirectDebit(DirectDebitInitiation initiation, Pain008Version version, Pain00xWriterOptions options)
    {
        var isCurrentVersion = PainWriterHelpers.IsCurrentVersion(version);
        var nameMaxLength = PainWriterHelpers.MaxNameLength(version);

        ValidateId(initiation.MessageId, "MessageId", options);
        ValidateName(initiation.InitiatingParty.Name, nameMaxLength, "InitiatingParty.Name", options);
        ValidateParty(initiation.InitiatingParty, "InitiatingParty", options, isCurrentVersion);

        if (initiation.PaymentInformations.Count == 0)
            throw new Iso20022ValidationException("At least one payment information block is required.", "PaymentInformations");

        for (var i = 0; i < initiation.PaymentInformations.Count; i++)
        {
            var pmtInf = initiation.PaymentInformations[i];
            var pmtInfPath = $"PaymentInformations[{i}]";

            ValidateId(pmtInf.PaymentInformationId, $"{pmtInfPath}.PaymentInformationId", options);
            ValidateEnumDefined(pmtInf.Scheme, $"{pmtInfPath}.Scheme");
            ValidateEnumDefined(pmtInf.SequenceType, $"{pmtInfPath}.SequenceType");
            if (pmtInf.ChargeBearer is { } pmtInfChargeBearer)
                ValidateEnumDefined(pmtInfChargeBearer, $"{pmtInfPath}.ChargeBearer");
            ValidateOptionalId(pmtInf.ServiceLevelCode, $"{pmtInfPath}.ServiceLevelCode", options, ServiceLevelCodeMaxLength);
            ValidateOptionalId(pmtInf.CategoryPurposeCode, $"{pmtInfPath}.CategoryPurposeCode", options, CategoryPurposeCodeMaxLength);
            ValidateName(pmtInf.Creditor.Name, nameMaxLength, $"{pmtInfPath}.Creditor.Name", options);
            ValidateParty(pmtInf.Creditor, $"{pmtInfPath}.Creditor", options, isCurrentVersion);
            ValidateOptionalName(pmtInf.UltimateCreditor?.Name, nameMaxLength, $"{pmtInfPath}.UltimateCreditor.Name", options);
            ValidateParty(pmtInf.UltimateCreditor, $"{pmtInfPath}.UltimateCreditor", options, isCurrentVersion);
            ValidateAccount(pmtInf.CreditorAccount, $"{pmtInfPath}.CreditorAccount", options);
            ValidateOptionalBic(pmtInf.CreditorAgent?.Bic, $"{pmtInfPath}.CreditorAgent.Bic", isCurrentVersion);
            ValidateId(pmtInf.CreditorSchemeId, $"{pmtInfPath}.CreditorSchemeId", options);

            if (pmtInf.Transactions.Count == 0)
                throw new Iso20022ValidationException("A payment information block requires at least one transaction.", $"{pmtInfPath}.Transactions");

            for (var j = 0; j < pmtInf.Transactions.Count; j++)
            {
                var transaction = pmtInf.Transactions[j];
                var txPath = $"{pmtInfPath}.Transactions[{j}]";

                ValidateOptionalId(transaction.InstructionId, $"{txPath}.InstructionId", options);
                ValidateId(transaction.EndToEndId, $"{txPath}.EndToEndId", options);
                ValidateAmount(transaction.Amount, $"{txPath}.Amount");
                ValidateId(transaction.Mandate.MandateId, $"{txPath}.Mandate.MandateId", options);
                if (transaction.Mandate.DateOfSignature is null)
                    throw new Iso20022ValidationException("The mandate's date of signature is required.", $"{txPath}.Mandate.DateOfSignature");

                if (transaction.Mandate.AmendmentIndicator == true)
                {
                    ValidateOptionalId(transaction.Mandate.OriginalMandateId, $"{txPath}.Mandate.OriginalMandateId", options);
                    ValidateOptionalId(transaction.Mandate.OriginalCreditorSchemeId, $"{txPath}.Mandate.OriginalCreditorSchemeId", options);
                    ValidateOptionalName(transaction.Mandate.OriginalCreditorName, nameMaxLength, $"{txPath}.Mandate.OriginalCreditorName", options);
                    if (transaction.Mandate.OriginalDebtorAccount is { } originalDebtorAccount)
                        ValidateAccount(originalDebtorAccount, $"{txPath}.Mandate.OriginalDebtorAccount", options);
                    ValidateOptionalBic(transaction.Mandate.OriginalDebtorAgent?.Bic, $"{txPath}.Mandate.OriginalDebtorAgent.Bic", isCurrentVersion);
                }

                ValidateName(transaction.Debtor.Name, nameMaxLength, $"{txPath}.Debtor.Name", options);
                ValidateParty(transaction.Debtor, $"{txPath}.Debtor", options, isCurrentVersion);
                ValidateOptionalName(transaction.UltimateDebtor?.Name, nameMaxLength, $"{txPath}.UltimateDebtor.Name", options);
                ValidateParty(transaction.UltimateDebtor, $"{txPath}.UltimateDebtor", options, isCurrentVersion);
                ValidateAccount(transaction.DebtorAccount, $"{txPath}.DebtorAccount", options);
                ValidateOptionalBic(transaction.DebtorAgent?.Bic, $"{txPath}.DebtorAgent.Bic", isCurrentVersion);
                ValidateOptionalId(transaction.PurposeCode, $"{txPath}.PurposeCode", options, PurposeCodeMaxLength);
                ValidateRemittance(transaction.RemittanceInformation, pmtInf.ServiceLevelCode, $"{txPath}.RemittanceInformation", options);
            }

            ValidateControlSum(pmtInf.ControlSum, $"{pmtInfPath}.ControlSum");
        }

        ValidateControlSum(initiation.ControlSum, "ControlSum");
    }

    private static void ValidateId(string? value, string path, Pain00xWriterOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Iso20022ValidationException("An identifier is required.", path);
        if (value.Length > IdMaxLength)
            throw new Iso20022ValidationException($"An identifier must not exceed {IdMaxLength} characters.", path);
        ValidateCharacterSet(value, path, options);
    }

    /// <summary>Validates that <paramref name="value"/> is null or a non-blank identifier of at most <paramref name="maxLength"/> characters.</summary>
    private static void ValidateOptionalId(string? value, string path, Pain00xWriterOptions options, int maxLength = IdMaxLength)
    {
        if (value is null)
            return;
        if (string.IsNullOrWhiteSpace(value))
            throw new Iso20022ValidationException("An identifier must not be empty or whitespace-only when set.", path);
        if (value.Length > maxLength)
            throw new Iso20022ValidationException($"An identifier must not exceed {maxLength} characters.", path);
        ValidateCharacterSet(value, path, options);
    }

    private static void ValidateName(string? name, int maxLength, string path, Pain00xWriterOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Iso20022ValidationException("A name is required.", path);
        if (name.Length > maxLength)
            throw new Iso20022ValidationException($"A name must not exceed {maxLength} characters.", path);
        ValidateCharacterSet(name, path, options);
    }

    private static void ValidateOptionalName(string? name, int maxLength, string path, Pain00xWriterOptions options)
    {
        if (name is null)
            return;
        if (string.IsNullOrWhiteSpace(name))
            throw new Iso20022ValidationException("A name must not be empty or whitespace-only when set.", path);
        if (name.Length > maxLength)
            throw new Iso20022ValidationException($"A name must not exceed {maxLength} characters.", path);
        ValidateCharacterSet(name, path, options);
    }

    /// <summary>Validates that <paramref name="value"/> is a defined member of <typeparamref name="TEnum"/>.</summary>
    private static void ValidateEnumDefined<TEnum>(TEnum value, string path) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(path, value, $"The {typeof(TEnum).Name} value is not defined.");
    }

    /// <summary>Validates the organisation/private identification of a party, e.g. an organisation BIC or the character set of scheme identifiers.</summary>
    private static void ValidateParty(PartyIdentification? party, string path, Pain00xWriterOptions options, bool isCurrentVersion)
    {
        if (party is null)
            return;

        if (party.OrganisationId is { } organisation)
        {
            ValidateOptionalBic(organisation.Bic, $"{path}.OrganisationId.Bic", isCurrentVersion);
            ValidateOptionalId(organisation.OtherId, $"{path}.OrganisationId.OtherId", options);
            ValidateOptionalId(organisation.OtherSchemeCode, $"{path}.OrganisationId.OtherSchemeCode", options);
            ValidateOptionalId(organisation.OtherSchemeProprietary, $"{path}.OrganisationId.OtherSchemeProprietary", options);
            ValidateOptionalId(organisation.OtherIssuer, $"{path}.OrganisationId.OtherIssuer", options);
        }

        if (party.PrivateId is { } privateId)
        {
            ValidateOptionalId(privateId.OtherId, $"{path}.PrivateId.OtherId", options);
            ValidateOptionalId(privateId.OtherSchemeCode, $"{path}.PrivateId.OtherSchemeCode", options);
            ValidateOptionalId(privateId.OtherSchemeProprietary, $"{path}.PrivateId.OtherSchemeProprietary", options);
            ValidateOptionalId(privateId.OtherIssuer, $"{path}.PrivateId.OtherIssuer", options);
        }
    }

    /// <summary>
    /// Validates that an account carries exactly one of an IBAN or another identifier (at most <see cref="OtherIdMaxLength"/>
    /// characters), that a set IBAN is well-formed, and that a set currency is a 3-letter uppercase ISO 4217 code.
    /// </summary>
    private static void ValidateAccount(AccountIdentification account, string path, Pain00xWriterOptions options)
    {
        var hasIban = !string.IsNullOrWhiteSpace(account.Iban);
        var hasOtherId = account.OtherId is not null;

        if (hasOtherId)
            ValidateOptionalId(account.OtherId, $"{path}.OtherId", options, OtherIdMaxLength);

        if (!hasIban && !hasOtherId)
            throw new Iso20022ValidationException("An account requires either an IBAN or another identifier.", path);
        if (hasIban && hasOtherId)
            throw new Iso20022ValidationException("An account must not carry both an IBAN and another identifier.", path);

        if (hasIban)
            ValidateIbanFormat(account.Iban!, $"{path}.Iban");

        if (account.Currency is { } currency && !CurrencyFormat.IsMatch(currency))
            throw new Iso20022ValidationException("A currency must be a 3-letter uppercase ISO 4217 code.", $"{path}.Currency");
    }

    private static void ValidateAmount(Money amount, string path)
    {
        if (amount.Amount <= 0)
            throw new Iso20022ValidationException("An amount must be greater than zero.", path);
        if (decimal.Round(amount.Amount, 2) != amount.Amount)
            throw new Iso20022ValidationException("An amount must not carry more than two decimal places.", path);
        if (CountTotalDigits(amount.Amount) > AmountMaxTotalDigits)
            throw new Iso20022ValidationException($"An amount must not carry more than {AmountMaxTotalDigits} total digits.", path);

        if (amount.Currency is not { } currency || !CurrencyFormat.IsMatch(currency))
            throw new Iso20022ValidationException("A currency must be a 3-letter uppercase ISO 4217 code.", $"{path}.Currency");
    }

    /// <summary>Validates that a control sum (the total of one or more amounts) does not exceed the XSD's total-digits limit.</summary>
    private static void ValidateControlSum(decimal controlSum, string path)
    {
        if (CountTotalDigits(controlSum) > AmountMaxTotalDigits)
            throw new Iso20022ValidationException($"A control sum must not carry more than {AmountMaxTotalDigits} total digits.", path);
    }

    /// <summary>Counts the digit characters of an amount as it would be written to XML (exactly two decimal places, no sign).</summary>
    private static int CountTotalDigits(decimal amount) =>
        PainWriterHelpers.FormatAmount(Math.Abs(amount)).Count(char.IsDigit);

    /// <summary>Validates the format and check digits of an IBAN that is already known to be present.</summary>
    private static void ValidateIbanFormat(string iban, string path)
    {
        var normalized = PainWriterHelpers.NormalizeIban(iban);
        if (!IbanFormat.IsMatch(normalized))
            throw new Iso20022ValidationException("An IBAN must match the pattern [A-Z]{2}[0-9]{2}[A-Z0-9]{1,30} after normalization.", path);

        if (!HasValidIbanCheckDigits(normalized))
            throw new Iso20022ValidationException("The IBAN check digits are invalid.", path);
    }

    /// <summary>Validates a BIC against the version-appropriate pattern: the current schemas allow digits in positions 1-4, the legacy schemas do not.</summary>
    private static void ValidateOptionalBic(string? bic, string path, bool isCurrentVersion)
    {
        if (bic is null)
            return;

        if (isCurrentVersion)
        {
            if (!BicFormatCurrent.IsMatch(bic))
                throw new Iso20022ValidationException("A BIC must match the pattern [A-Z0-9]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?.", path);
        }
        else
        {
            if (!BicFormatLegacy.IsMatch(bic))
                throw new Iso20022ValidationException("A BIC must match the pattern [A-Z]{6}[A-Z2-9][A-NP-Z0-9]([A-Z0-9]{3})?.", path);
        }
    }

    private static void ValidateRemittance(RemittanceInformation? remittance, string? serviceLevelCode, string path, Pain00xWriterOptions options)
    {
        if (remittance is null)
            return;

        var hasUnstructured = remittance.Unstructured.Count > 0;
        var hasReference = remittance.CreditorReference is not null;
        var hasStructured = hasReference || remittance.CreditorReferenceTypeCode is not null
            || remittance.CreditorReferenceTypeProprietary is not null || remittance.CreditorReferenceIssuer is not null;

        if (hasUnstructured && hasStructured)
            throw new Iso20022ValidationException("Remittance information must carry either unstructured lines or a creditor reference, not both.", path);

        if (hasUnstructured)
        {
            var requiresSingleLine = serviceLevelCode is null or "SEPA";
            if (requiresSingleLine && remittance.Unstructured.Count > 1)
                throw new Iso20022ValidationException("SEPA payments carry exactly one unstructured remittance information line.", $"{path}.Unstructured");

            for (var i = 0; i < remittance.Unstructured.Count; i++)
            {
                var line = remittance.Unstructured[i];
                if (line.Length > RemittanceLineMaxLength)
                    throw new Iso20022ValidationException($"An unstructured remittance information line must not exceed {RemittanceLineMaxLength} characters.", $"{path}.Unstructured[{i}]");
                ValidateCharacterSet(line, $"{path}.Unstructured[{i}]", options);
            }
        }

        if (hasReference)
        {
            if (string.IsNullOrWhiteSpace(remittance.CreditorReference))
                throw new Iso20022ValidationException("A creditor reference must not be empty or whitespace-only when set.", $"{path}.CreditorReference");
            if (remittance.CreditorReference!.Length > IdMaxLength)
                throw new Iso20022ValidationException($"A creditor reference must not exceed {IdMaxLength} characters.", $"{path}.CreditorReference");
            ValidateCharacterSet(remittance.CreditorReference, $"{path}.CreditorReference", options);
        }

        if (remittance.CreditorReferenceTypeCode is not null && remittance.CreditorReferenceTypeProprietary is not null)
            throw new Iso20022ValidationException("A creditor reference type must not carry both a code and a proprietary value.", $"{path}.CreditorReferenceTypeProprietary");

        // CdtrRefInf/Tp and CdtrRefInf/Ref are independently optional per the XSD, so a type/issuer without a
        // reference is validated, not rejected.
        ValidateCreditorReferenceTypeCode(remittance.CreditorReferenceTypeCode, $"{path}.CreditorReferenceTypeCode");
        ValidateOptionalId(remittance.CreditorReferenceTypeProprietary, $"{path}.CreditorReferenceTypeProprietary", options);
        ValidateOptionalId(remittance.CreditorReferenceIssuer, $"{path}.CreditorReferenceIssuer", options);
    }

    /// <summary>Validates that <paramref name="code"/>, when set, is a defined ISO 20022 <c>DocumentType3Code</c> value.</summary>
    private static void ValidateCreditorReferenceTypeCode(string? code, string path)
    {
        if (code is not null && Array.IndexOf(CreditorReferenceTypeCodes, code) < 0)
            throw new Iso20022ValidationException($"A creditor reference type code must be one of: {string.Join(", ", CreditorReferenceTypeCodes)}.", path);
    }

    private static void ValidateCharacterSet(string value, string path, Pain00xWriterOptions options)
    {
        if (options.ValidateCharacterSet && !SepaCharacterSet.IsValid(value))
            throw new Iso20022ValidationException("The value contains characters outside the SEPA character set.", path);
    }

    /// <summary>Validates the ISO 7064 MOD 97-10 check digits of an already-normalized IBAN.</summary>
    private static bool HasValidIbanCheckDigits(string normalizedIban)
    {
        var rearranged = normalizedIban[4..] + normalizedIban[..4];

        var remainder = 0;
        foreach (var c in rearranged)
        {
            var value = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            foreach (var digitChar in value.ToString(CultureInfo.InvariantCulture))
                remainder = ((remainder * 10) + (digitChar - '0')) % 97;
        }

        return remainder == 1;
    }
}
