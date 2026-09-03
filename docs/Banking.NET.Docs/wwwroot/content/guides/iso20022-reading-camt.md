---
title: "ISO 20022: Reading camt Messages"
category: Guides
order: 5
description: Parsing camt.052, camt.053 and camt.054 messages with CamtReader.
---

## CamtReader

`CamtReader` reads camt.052 (intraday account report), camt.053 (account statement) and camt.054
(debit/credit notification) messages — one model covers all three, since they share the same
structure. Four overloads cover the ways a message content can arrive:

```csharp
public static class CamtReader
{
    public static BankToCustomerMessage Read(XDocument document);
    public static BankToCustomerMessage Read(Stream xml);
    public static BankToCustomerMessage Read(string xml);
    public static BankToCustomerMessage Read(CorporatePaymentsMessage message);
}
```

The `CorporatePaymentsMessage` overload is the most convenient entry point when reading a message
just downloaded from the API — it calls `message.GetContentAsString()` internally, so gzip
decompression and encoding detection are handled for you:

```csharp
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;

var message = await client.DownloadMessageAsync(messageInfo);
var statement = CamtReader.Read(message);
```

For a message you are streaming rather than buffering (see
[Downloading Messages](/docs/guides/downloading-messages)), use `Read(Stream)` directly on the
(decompressed) stream instead.

## The Model

```csharp
public enum StatementKind { Report, Statement, Notification }

public sealed class BankToCustomerMessage { Iso20022MessageIdentifier Identifier; GroupHeader GroupHeader; List<AccountStatement> Statements; XElement Source }
public sealed class GroupHeader { string? MessageId; DateTimeOffset? CreationDateTime; string? MessageRecipientName; int? PageNumber; bool? LastPageIndicator; string? AdditionalInformation; XElement Source }
public sealed class AccountStatement { StatementKind Kind; string? Id; AccountIdentification? Account; List<Balance> Balances; TransactionsSummary? TransactionsSummary; List<StatementEntry> Entries; /* ... */ }
public sealed class StatementEntry { string? EntryReference; Money Amount; CreditDebitIndicator CreditDebit; DateOnly? BookingDate; DateTimeOffset? BookingDateTime; List<EntryDetails> Details; /* ... */ }
```

`StatementKind` tells you which of the three message types produced a given `AccountStatement`:
`Report` for camt.052, `Statement` for camt.053, `Notification` for camt.054.

```csharp
foreach (var account in statement.Statements)
{
    Console.WriteLine($"{account.Kind}: IBAN {account.Account?.Iban}");

    foreach (var entry in account.Entries)
        Console.WriteLine($"{entry.BookingDate} {entry.CreditDebit} {entry.Amount.Amount} {entry.Amount.Currency}");
}
```

Every model class also carries a `Source` property — the `XElement` it was read from — so fields the
model does not expose explicitly are still reachable.

## Missing Optional Elements Never Throw

Camt schemas evolved across versions (.02 and .08 both occur in the wild), and many elements are
optional to begin with. `CamtReader` reflects this: a missing optional element becomes `null` (or an
empty list for repeatable elements) on the model — it never throws. Only two kinds of problems raise
an exception:

- **Structural errors** — the document's root element is not `Document`, or none of the expected
  message children (`BkToCstmrAcctRpt`, `BkToCstmrStmt`, `BkToCstmrDbtCdtNtfctn`) is present.
- **Missing required elements** behind a non-nullable property, e.g. an entry's `Amt` or `CdtDbtInd`.

Both raise `Iso20022ValidationException` with `Path` set to the offending element, e.g. `"Document"`,
`"Document/BkToCstmrStmt"`, `"Bal/Amt"`, or `"Ntry/CdtDbtInd"`:

```csharp
try
{
    var statement = CamtReader.Read(xml);
}
catch (Iso20022ValidationException ex)
{
    Console.WriteLine($"{ex.Message} (at {ex.Path})");
}
```

## Currency Fallback

Some amount elements in camt.052/053/054 (e.g. the net entry total on a statement summary) carry no
`Ccy` attribute of their own. In that case, `CamtReader` falls back to the account's own currency
(`Acct/Ccy` on the surrounding statement). If the account carries no currency either, `Money.Currency`
is left as an empty string rather than guessed.

## Next Steps

- [ISO 20022: Reading pain.002 Messages](/docs/guides/iso20022-reading-pain002)
- [Downloading Messages](/docs/guides/downloading-messages)
