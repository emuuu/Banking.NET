---
title: "ISO 20022: Reading pain.002 Messages"
category: Guides
order: 6
description: Parsing payment status reports with Pain002Reader.
---

## Pain002Reader

`Pain002Reader` reads pain.002 (customer payment status report) messages — the bank's way of
reporting the outcome of a previously submitted pain.001 or pain.008 order, or of a batch of SEPA
transactions in general. The same four-overload shape as `CamtReader` is available:

```csharp
public static class Pain002Reader
{
    public static PaymentStatusReport Read(XDocument document);
    public static PaymentStatusReport Read(Stream xml);
    public static PaymentStatusReport Read(string xml);
    public static PaymentStatusReport Read(CorporatePaymentsMessage message);
}
```

```csharp
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;

var message = await client.DownloadMessageAsync(messageInfo);
var report = Pain002Reader.Read(message);
```

## Which Order Types Deliver pain.002

Several download order types carry a pain.002 payload — see [Order Types](/docs/guides/order-types)
for the full table:

| Order type | Reports on |
|---|---|
| `HAC` | Customer acknowledgement — general status report |
| `AXS` | Cross-border credit transfers |
| `CUZ` | Urgent credit transfers |
| `XIP` | Orders originally submitted as `XIC` or `XID` |
| `CIZ` | Instant credit transfers |
| `CDZ` | Direct debits |
| `CRZ` | Credit transfers |

A `HAC` message, for example, arrives some time after a `CCT` order was submitted, and its content —
read via `Pain002Reader` — tells you whether that order was accepted, rejected, or partially
processed.

## The Model

```csharp
public sealed class PaymentStatusReport
{
    Iso20022MessageIdentifier Identifier;
    string? MessageId;
    DateTimeOffset? CreationDateTime;
    PartyIdentification? InitiatingParty;
    FinancialInstitution? DebtorAgent;
    FinancialInstitution? CreditorAgent;
    OriginalGroupStatus OriginalGroup;
    List<OriginalPaymentInformationStatus> PaymentInformations;
    XElement Source;
}

public sealed class OriginalGroupStatus
{
    string? OriginalMessageId;
    string? OriginalMessageNameId;
    string? GroupStatus;                 // e.g. ACTC, RJCT, ACSP — the group-level status
    List<StatusReason> StatusReasons;
    List<TransactionCountPerStatus> NumberOfTransactionsPerStatus;
}

public sealed class OriginalPaymentInformationStatus
{
    string? OriginalPaymentInformationId;
    string? PaymentInformationStatus;    // the per-PaymentInformation status
    List<TransactionStatus> Transactions;
}

public sealed class TransactionStatus
{
    string? OriginalInstructionId;
    string? OriginalEndToEndId;
    string? Status;                      // TransactionStatus, per individual transaction
    List<StatusReason> StatusReasons;
    OriginalTransactionReference? OriginalTransaction;
}
```

The report is layered exactly like the original order was: an overall `OriginalGroup` status, a
`PaymentInformationStatus` per `PmtInf` batch, and a `Status` per individual transaction inside
`TransactionStatus`. A rejection at the group level (`OriginalGroup.GroupStatus == "RJCT"`) typically
means none of the transactions were processed; a per-transaction rejection means the rest of the
batch may still have gone through.

```csharp
Console.WriteLine($"Group status: {report.OriginalGroup.GroupStatus}");

foreach (var paymentInformation in report.PaymentInformations)
{
    Console.WriteLine($"  {paymentInformation.OriginalPaymentInformationId}: {paymentInformation.PaymentInformationStatus}");

    foreach (var transaction in paymentInformation.Transactions)
    {
        Console.WriteLine($"    {transaction.OriginalEndToEndId}: {transaction.Status}");
        foreach (var reason in transaction.StatusReasons)
            Console.WriteLine($"      Reason: {reason.Code ?? reason.Proprietary}");
    }
}
```

As with `CamtReader`, missing optional elements become `null` or an empty list rather than throwing;
a missing structural element (root not `Document`, no `CstmrPmtStsRpt` child) or a missing required
element behind a non-nullable property throws `Iso20022ValidationException` with `Path` set.

## Next Steps

- [ISO 20022: Reading camt Messages](/docs/guides/iso20022-reading-camt)
- [ISO 20022: Writing pain.001 / pain.008](/docs/guides/iso20022-writing-pain)
- [Submitting Orders](/docs/guides/submitting-orders)
