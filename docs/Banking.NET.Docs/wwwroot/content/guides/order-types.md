---
title: Order Types
category: Guides
order: 1
description: The OrderType value type and the full set of Corporate Payments order types.
---

## The OrderType Value Type

`OrderType` is a readonly struct, not an enum. The Corporate Payments API is built on EBICS order
types, and which ones are actually available to a given customer depends on the agreement with the
bank — including customer-specific codes that are not publicly documented. A value type with known
constants and an open parser models this better than a closed enum would:

```csharp
public readonly struct OrderType : IEquatable<OrderType>
{
    public string Code { get; }
    public OrderDirection Direction { get; }
    public string? MessageType { get; }
    public IReadOnlyList<string> SchemaVersions { get; }
    public string? Description { get; }
    public bool IsKnown { get; }

    public static OrderType Parse(string code);
    public static bool TryParse(string? code, out OrderType orderType);

    public static IReadOnlyList<OrderType> Known { get; }
    public static IReadOnlyList<OrderType> DownloadTypes { get; }
    public static IReadOnlyList<OrderType> UploadTypes { get; }
}
```

- `Code` is the upper-case order type code, e.g. `"C53"` or `"CCT"`.
- `Direction` is `OrderDirection.Download`, `OrderDirection.Upload`, or `OrderDirection.Unknown` for
  a code that is not one of the documented types.
- `MessageType` is the ISO 20022 message family the order type carries (e.g. `"camt.053"`,
  `"pain.001"`), or `null` if unknown.
- `SchemaVersions` lists the ISO 20022 schema versions the order type may carry, most recent first.
- `IsKnown` is `true` exactly when `Direction != OrderDirection.Unknown`.

Each documented order type is also exposed as a static property, e.g. `OrderType.C53`,
`OrderType.CCT`.

### Parsing Codes

```csharp
var orderType = OrderType.Parse("c53");   // trimmed and upper-cased: "C53", IsKnown == true
var known = OrderType.C53 == orderType;   // true — comparison is case-insensitive by Code

OrderType.TryParse("XYZ", out var unknown);
// unknown.Code == "XYZ", unknown.Direction == OrderDirection.Unknown, unknown.IsKnown == false
```

An unknown code is **not rejected** — it parses successfully with `Direction` set to
`OrderDirection.Unknown`. This lets you use order types your bank has enabled for you specifically,
even if they are not among the documented ones. `Parse` throws `ArgumentException` only for a null,
empty, or white-space code.

## Documented Order Types

| Code | Direction | Message type | Schema versions | Description |
|---|---|---|---|---|
| C52 | Download | camt.052 | camt.052.001.08, camt.052.001.02 | Intraday account report |
| C53 | Download | camt.053 | camt.053.001.08, camt.053.001.02 | Account statement |
| C54 | Download | camt.054 | camt.054.001.08 | Debit and credit notification |
| C86 | Download | camt.086 | camt.086.001.02, camt.086.001.01 | Bank services billing statement |
| HAC | Download | pain.002 | pain.002.001.10, pain.002.001.03 | Customer acknowledgement (payment status report) |
| AXS | Download | pain.002 | pain.002.001.10 | Payment status report for cross-border credit transfers |
| CUZ | Download | pain.002 | pain.002.001.10 | Payment status report for urgent credit transfers |
| XIP | Download | pain.002 | pain.002.001.03, pain.002.001.10 | Payment status report for XIC and XID orders |
| CIZ | Download | pain.002 | pain.002.001.10 | Payment status report for instant credit transfers |
| CDZ | Download | pain.002 | pain.002.001.10 | Payment status report for direct debits |
| CRZ | Download | pain.002 | pain.002.001.10 | Payment status report for credit transfers |
| CCT | Upload | pain.001 | pain.001.001.09 | SEPA credit transfer |
| CTV | Upload | pain.001 | pain.001.001.09 | SEPA credit transfer (CTV) |
| CIP | Upload | pain.001 | pain.001.001.09 | SEPA instant credit transfer |
| CIV | Upload | pain.001 | pain.001.001.09 | SEPA instant credit transfer (CIV) |
| XIC | Upload | pain.001 | pain.001.001.03 | SEPA credit transfer (pain.001.001.03) |
| CDD | Upload | pain.008 | pain.008.001.08 | SEPA direct debit (CORE) |
| CDB | Upload | pain.008 | pain.008.001.08 | SEPA direct debit (B2B) |
| XID | Upload | pain.008 | pain.008.001.02 | SEPA direct debit (pain.008.001.02) |
| AXZ | Upload | pain.001 | pain.001.001.09 | Cross-border credit transfer |
| CCU | Upload | pain.001 | pain.001.001.09 | Urgent credit transfer |

Availability of any given order type depends on the agreement between the customer and Commerzbank;
not every order type in this table is necessarily enabled for a given client.

## Filtering by Direction

```csharp
foreach (var downloadType in OrderType.DownloadTypes)
    Console.WriteLine($"{downloadType.Code}: {downloadType.Description}");

foreach (var uploadType in OrderType.UploadTypes)
    Console.WriteLine($"{uploadType.Code}: {uploadType.Description}");
```

`ListMessagesAsync` and `DownloadMessageAsync` accept any order type, but `SubmitOrderAsync` throws
`ArgumentException` when passed a download order type — see
[Submitting Orders](/docs/guides/submitting-orders).

## Next Steps

- [Downloading Messages](/docs/guides/downloading-messages)
- [Submitting Orders](/docs/guides/submitting-orders)
