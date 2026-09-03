using System.Globalization;
using System.Text.RegularExpressions;

namespace Commerzbank.NET.CorporatePayments.Iso20022.Internal;

/// <summary>Validates pain.001/pain.008 writer models before <see cref="Pain001Writer"/>/<see cref="Pain008Writer"/> render them to XML.</summary>
/// <remarks>Failure paths use C# property navigation, e.g. <c>PaymentInformations[0].Transactions[2].EndToEndId</c>, not XML element paths.</remarks>
internal static class PainValidator
{
    private const int IdMaxLength = 35;
    private const int RemittanceLineMaxLength = 140;
    private const int AmountMaxTotalDigits = 18;

    private static readonly Regex IbanFormat = new("^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BicFormat = new("^[A-Z0-9]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CurrencyFormat = new("^[A-Z]{3}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Validates a pain.001 credit transfer initiation.</summary>
    /// <exception cref="Iso20022ValidationException">The initiation fails a validation rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    public static void ValidateCreditTransfer(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions options)
    {
        var nameMaxLength = PainWriterHelpers.MaxNameLength(version);

        ValidateId(initiation.MessageId, "MessageId", options);
        ValidateName(initiation.InitiatingParty.Name, nameMaxLength, "InitiatingParty.Name", options);
        ValidateParty(initiation.InitiatingParty, "InitiatingParty", options);

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
            ValidateName(pmtInf.Debtor.Name, nameMaxLength, $"{pmtInfPath}.Debtor.Name", options);
            ValidateParty(pmtInf.Debtor, $"{pmtInfPath}.Debtor", options);
            ValidateOptionalName(pmtInf.UltimateDebtor?.Name, nameMaxLength, $"{pmtInfPath}.UltimateDebtor.Name", options);
            ValidateParty(pmtInf.UltimateDebtor, $"{pmtInfPath}.UltimateDebtor", options);
            ValidateAccount(pmtInf.DebtorAccount, $"{pmtInfPath}.DebtorAccount");
            ValidateOptionalBic(pmtInf.DebtorAgent?.Bic, $"{pmtInfPath}.DebtorAgent.Bic");

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
                ValidateParty(transaction.Creditor, $"{txPath}.Creditor", options);
                ValidateOptionalName(transaction.UltimateDebtor?.Name, nameMaxLength, $"{txPath}.UltimateDebtor.Name", options);
                ValidateParty(transaction.UltimateDebtor, $"{txPath}.UltimateDebtor", options);
                ValidateOptionalName(transaction.UltimateCreditor?.Name, nameMaxLength, $"{txPath}.UltimateCreditor.Name", options);
                ValidateParty(transaction.UltimateCreditor, $"{txPath}.UltimateCreditor", options);
                ValidateAccount(transaction.CreditorAccount, $"{txPath}.CreditorAccount");
                ValidateOptionalBic(transaction.CreditorAgent?.Bic, $"{txPath}.CreditorAgent.Bic");
                ValidateRemittance(transaction.RemittanceInformation, pmtInf.ServiceLevelCode, $"{txPath}.RemittanceInformation", options);
            }
        }
    }

    /// <summary>Validates a pain.008 direct debit initiation.</summary>
    /// <exception cref="Iso20022ValidationException">The initiation fails a validation rule.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/>, or an enum value within <paramref name="initiation"/>, is not defined.</exception>
    public static void ValidateDirectDebit(DirectDebitInitiation initiation, Pain008Version version, Pain00xWriterOptions options)
    {
        var nameMaxLength = PainWriterHelpers.MaxNameLength(version);

        ValidateId(initiation.MessageId, "MessageId", options);
        ValidateName(initiation.InitiatingParty.Name, nameMaxLength, "InitiatingParty.Name", options);
        ValidateParty(initiation.InitiatingParty, "InitiatingParty", options);

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
            ValidateName(pmtInf.Creditor.Name, nameMaxLength, $"{pmtInfPath}.Creditor.Name", options);
            ValidateParty(pmtInf.Creditor, $"{pmtInfPath}.Creditor", options);
            ValidateOptionalName(pmtInf.UltimateCreditor?.Name, nameMaxLength, $"{pmtInfPath}.UltimateCreditor.Name", options);
            ValidateParty(pmtInf.UltimateCreditor, $"{pmtInfPath}.UltimateCreditor", options);
            ValidateAccount(pmtInf.CreditorAccount, $"{pmtInfPath}.CreditorAccount");
            ValidateOptionalBic(pmtInf.CreditorAgent?.Bic, $"{pmtInfPath}.CreditorAgent.Bic");
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
                    if (transaction.Mandate.OriginalDebtorAccount?.Iban is { } originalDebtorIban)
                        ValidateIbanFormat(originalDebtorIban, $"{txPath}.Mandate.OriginalDebtorAccount.Iban");
                    ValidateOptionalBic(transaction.Mandate.OriginalDebtorAgent?.Bic, $"{txPath}.Mandate.OriginalDebtorAgent.Bic");
                }

                ValidateName(transaction.Debtor.Name, nameMaxLength, $"{txPath}.Debtor.Name", options);
                ValidateParty(transaction.Debtor, $"{txPath}.Debtor", options);
                ValidateOptionalName(transaction.UltimateDebtor?.Name, nameMaxLength, $"{txPath}.UltimateDebtor.Name", options);
                ValidateParty(transaction.UltimateDebtor, $"{txPath}.UltimateDebtor", options);
                ValidateAccount(transaction.DebtorAccount, $"{txPath}.DebtorAccount");
                ValidateOptionalBic(transaction.DebtorAgent?.Bic, $"{txPath}.DebtorAgent.Bic");
                ValidateRemittance(transaction.RemittanceInformation, pmtInf.ServiceLevelCode, $"{txPath}.RemittanceInformation", options);
            }
        }
    }

    private static void ValidateId(string? value, string path, Pain00xWriterOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Iso20022ValidationException("An identifier is required.", path);
        if (value.Length > IdMaxLength)
            throw new Iso20022ValidationException($"An identifier must not exceed {IdMaxLength} characters.", path);
        ValidateCharacterSet(value, path, options);
    }

    private static void ValidateOptionalId(string? value, string path, Pain00xWriterOptions options)
    {
        if (value is null)
            return;
        if (string.IsNullOrWhiteSpace(value))
            throw new Iso20022ValidationException("An identifier must not be empty or whitespace-only when set.", path);
        if (value.Length > IdMaxLength)
            throw new Iso20022ValidationException($"An identifier must not exceed {IdMaxLength} characters.", path);
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
    private static void ValidateParty(PartyIdentification? party, string path, Pain00xWriterOptions options)
    {
        if (party is null)
            return;

        if (party.OrganisationId is { } organisation)
        {
            ValidateOptionalBic(organisation.Bic, $"{path}.OrganisationId.Bic");
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

    /// <summary>Validates that an account carries exactly one of an IBAN or another identifier, and that a set IBAN is well-formed.</summary>
    private static void ValidateAccount(AccountIdentification account, string path)
    {
        var hasIban = !string.IsNullOrWhiteSpace(account.Iban);
        var hasOtherId = !string.IsNullOrWhiteSpace(account.OtherId);

        if (!hasIban && !hasOtherId)
            throw new Iso20022ValidationException("An account requires either an IBAN or another identifier.", path);
        if (hasIban && hasOtherId)
            throw new Iso20022ValidationException("An account must not carry both an IBAN and another identifier.", path);

        if (hasIban)
            ValidateIbanFormat(account.Iban!, $"{path}.Iban");
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

    private static void ValidateOptionalBic(string? bic, string path)
    {
        if (bic is null)
            return;
        if (!BicFormat.IsMatch(bic))
            throw new Iso20022ValidationException("A BIC must match the pattern [A-Z0-9]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?.", path);
    }

    private static void ValidateRemittance(RemittanceInformation? remittance, string? serviceLevelCode, string path, Pain00xWriterOptions options)
    {
        if (remittance is null)
            return;

        var hasUnstructured = remittance.Unstructured.Count > 0;
        var hasStructured = !string.IsNullOrEmpty(remittance.CreditorReference);

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

        if (hasStructured)
        {
            if (remittance.CreditorReference!.Length > IdMaxLength)
                throw new Iso20022ValidationException($"A creditor reference must not exceed {IdMaxLength} characters.", $"{path}.CreditorReference");
            ValidateCharacterSet(remittance.CreditorReference, $"{path}.CreditorReference", options);
        }
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
