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
[Downloading Messages](docs/guides/downloading-messages)), use `Read(Stream)` directly on the
(decompressed) stream instead.

## The Model

```csharp
public enum StatementKind { Report, Statement, Notification }

public sealed class BankToCustomerMessage
{
    public required Iso20022MessageIdentifier Identifier { get; init; }
    public required GroupHeader GroupHeader { get; init; }
    public List<AccountStatement> Statements { get; init; } = [];
    public required XElement Source { get; init; }
}

public sealed class GroupHeader
{
    public string? MessageId { get; init; }
    public DateTimeOffset? CreationDateTime { get; init; }
    public string? MessageRecipientName { get; init; }
    public int? PageNumber { get; init; }
    public bool? LastPageIndicator { get; init; }
    public string? AdditionalInformation { get; init; }
    public required XElement Source { get; init; }
}

public sealed class AccountStatement
{
    public required StatementKind Kind { get; init; }
    public string? Id { get; init; }
    public AccountIdentification? Account { get; init; }
    public List<Balance> Balances { get; init; } = [];
    public TransactionsSummary? TransactionsSummary { get; init; }
    public List<StatementEntry> Entries { get; init; } = [];
    // ...
    public required XElement Source { get; init; }
}

public sealed class StatementEntry
{
    public string? EntryReference { get; init; }
    public required Money Amount { get; init; }
    public required CreditDebitIndicator CreditDebit { get; init; }
    public DateOnly? BookingDate { get; init; }
    public DateTimeOffset? BookingDateTime { get; init; }
    public List<EntryDetails> Details { get; init; } = [];
    // ...
    public required XElement Source { get; init; }
}
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

The reader-specific model classes above — `BankToCustomerMessage`, `GroupHeader`,
`AccountStatement`, `StatementEntry` and their siblings such as `Balance`, `TransactionsSummary` and
`EntryDetails` — also carry a required `Source` property: the `XElement` they were read from, so
fields the model does not expose explicitly are still reachable. Types shared with the writers
(`Money`, `CreditDebitIndicator`, and similar `Iso20022/Common` types) are not all read-only, so this
does not extend to every type an entry references — `Money` carries no `Source` at all, and
`AccountIdentification.Source` is nullable rather than required.

## Missing Optional Elements Never Throw

Camt schemas evolved across versions (.02 and .08 both occur in the wild), and many elements are
optional to begin with. `CamtReader` reflects this once it has a parsed document: a missing optional
element becomes `null` (or an empty list for repeatable elements) on the model — it never throws.
Only two kinds of problems raise an exception at that point:

- **Structural errors** — the document's root element is not `Document`, or none of the expected
  message children (`BkToCstmrAcctRpt`, `BkToCstmrStmt`, `BkToCstmrDbtCdtNtfctn`) is present.
- **Missing required elements** behind a non-nullable property, e.g. an entry's `Amt` or `CdtDbtInd`.

Both raise `Iso20022ValidationException` with `Path` set to the offending element, e.g. `"Document"`,
`"Document/BkToCstmrStmt"`, `"Bal/Amt"`, or `"Ntry/CdtDbtInd"`. That covers `CamtReader.Read(XDocument)`
fully, but the `string`/`Stream` overloads first parse the input with `Iso20022Document.Load`, which
lets a plain `System.Xml.XmlException` propagate uncaught for input that is not even well-formed XML —
catch that separately if the input hasn't already been parsed and validated as XML:

```csharp
try
{
    var statement = CamtReader.Read(xml); // xml: string, not yet parsed
}
catch (Iso20022ValidationException ex)
{
    Console.WriteLine($"{ex.Message} (at {ex.Path})");
}
catch (System.Xml.XmlException ex)
{
    Console.WriteLine($"Not well-formed XML: {ex.Message}");
}
```

## Currency Fallback

Some amount elements in camt.052/053/054 (e.g. the net entry total on a statement summary) carry no
`Ccy` attribute of their own. In that case, `CamtReader` falls back to the account's own currency
(`Acct/Ccy` on the surrounding statement). If the account carries no currency either, `Money.Currency`
is left as an empty string rather than guessed.

## Next Steps

- [ISO 20022: Reading pain.002 Messages](docs/guides/iso20022-reading-pain002)
- [Downloading Messages](docs/guides/downloading-messages)
