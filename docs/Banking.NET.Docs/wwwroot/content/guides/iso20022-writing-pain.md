---
title: "ISO 20022: Writing pain.001 / pain.008"
category: Guides
order: 7
description: Building and validating credit transfer and direct debit orders with Pain001Writer and Pain008Writer.
---

## Pain001Writer and Pain008Writer

`Pain001Writer` writes pain.001 (customer credit transfer initiation) documents; `Pain008Writer`
writes pain.008 (customer direct debit initiation) documents. Both expose the same four output
shapes:

```csharp
public static class Pain001Writer
{
    public static XDocument Write(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null);
    public static string WriteToString(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null);
    public static byte[] WriteToBytes(CreditTransferInitiation initiation, Pain001Version version, Pain00xWriterOptions? options = null);
    public static void Write(CreditTransferInitiation initiation, Pain001Version version, Stream destination, Pain00xWriterOptions? options = null);
}
```

`Pain008Writer` mirrors this exactly, taking a `DirectDebitInitiation` and a `Pain008Version` instead.
`WriteToString` and `WriteToBytes` both encode as UTF-8 without a byte order mark — ready to hand
straight to `SubmitOrderAsync(orderType, xml)` or `SubmitOrderAsync(orderType, content)`.

## Building a Credit Transfer

The examples below use only the fixed test data documented for this library: the German test IBANs
`DE89370400440532013000`, `DE02120300000000202051` and `DE75512108001245126199` (used in the direct
debit example further down), the Commerzbank AG BIC `COBADEFFXXX`, and the placeholder names
`Example Debtor GmbH` / `Example Creditor Ltd`.

```csharp
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;

var initiation = new CreditTransferInitiation
{
    MessageId = "MSG-0001",
    InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
    PaymentInformations =
    [
        new CreditTransferPaymentInformation
        {
            PaymentInformationId = "PMT-0001",
            RequestedExecutionDate = DateOnly.FromDateTime(DateTime.Today),
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
            DebtorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" },
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

var xml = Pain001Writer.WriteToString(initiation, Pain001Version.V09);
var result = await client.SubmitOrderAsync(OrderType.CCT, xml);
```

`NumberOfTransactions` and `ControlSum` on `CreditTransferInitiation` and
`CreditTransferPaymentInformation` are computed, not settable — they are derived from the
transactions you add.

## Building a Direct Debit

```csharp
var initiation = new DirectDebitInitiation
{
    MessageId = "MSG-0002",
    InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
    PaymentInformations =
    [
        new DirectDebitPaymentInformation
        {
            PaymentInformationId = "PMT-0002",
            Scheme = DirectDebitScheme.Core,
            SequenceType = SequenceType.Recurring,
            RequestedCollectionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
            CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
            CreditorAgent = new FinancialInstitution { Bic = "COBADEFFXXX" },
            CreditorSchemeId = "DE98ZZZ09999999999",
            Transactions =
            [
                new DirectDebitTransaction
                {
                    EndToEndId = "E2E-0002",
                    Amount = new Money(50.00m, "EUR"),
                    Mandate = new MandateInformation
                    {
                        MandateId = "MANDATE-0001",
                        DateOfSignature = new DateOnly(2026, 1, 1),
                    },
                    Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                    DebtorAccount = new AccountIdentification { Iban = "DE75512108001245126199" },
                },
            ],
        },
    ],
};

var xml = Pain008Writer.WriteToString(initiation, Pain008Version.V08);
var result = await client.SubmitOrderAsync(OrderType.CDD, xml);
```

## Pain00xWriterOptions

```csharp
public sealed class Pain00xWriterOptions
{
    public bool ValidateCharacterSet { get; set; }   // default false
    public bool OmitXmlDeclaration { get; set; }     // default false
}
```

`ValidateCharacterSet` additionally checks names, unstructured remittance information, end-to-end
identifiers and other identifiers against the SEPA character set
(`SepaCharacterSet.IsValid`/`.Sanitize`). It is disabled by default — the writer never sanitizes text
on its own; if you need to strip diacritics and disallowed characters, call
`SepaCharacterSet.Sanitize(text)` yourself before assigning it to the model.

```csharp
var xml = Pain001Writer.WriteToString(initiation, Pain001Version.V09, new Pain00xWriterOptions
{
    ValidateCharacterSet = true,
});
```

## Validation

Both writers validate the object graph before producing any output — a failure means no bytes are
written at all. Failures throw `Iso20022ValidationException` with `Path` set to the offending
property, using a C#-style navigation path, e.g. `PaymentInformations[0].Transactions[2].EndToEndId`:

```csharp
try
{
    var xml = Pain001Writer.WriteToString(initiation, Pain001Version.V09);
}
catch (Iso20022ValidationException ex)
{
    Console.WriteLine($"{ex.Message} (at {ex.Path})");
}
```

Validation covers, among other things:

- Identifiers (`MessageId`, `PaymentInformationId`, `EndToEndId`, `InstructionId`, `MandateId`,
  `CreditorSchemeId`) at most 35 characters.
- Names at most 70 characters (schema versions .03/.02) or 140 characters (.09/.08).
- At least one `PaymentInformation`, and at least one transaction within it.
- Amounts greater than zero, with at most two decimal places.
- Currency codes matching `[A-Z]{3}`.
- Exactly one of IBAN or another account identifier set per account, with the IBAN checked for both
  format and checksum (mod 97) once normalized.
- BIC format, version-dependent (`BICFIDec2014Identifier` for the newer schema versions,
  `BICIdentifier` for the older ones).
- Optional identifiers and names that are set must be non-empty (never just whitespace-only) — the
  writer never emits an empty element for those. Unstructured remittance information lines are the
  exception: they are checked for length and character set, but not for being empty or
  whitespace-only, so a blank line is passed through as written.

## Next Steps

- [Submitting Orders](docs/guides/submitting-orders)
- [ISO 20022: Reading pain.002 Messages](docs/guides/iso20022-reading-pain002) — Reading back the status of a submitted order
