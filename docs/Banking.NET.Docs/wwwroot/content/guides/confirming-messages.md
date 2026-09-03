---
title: Confirming Messages
category: Guides
order: 3
description: Confirming receipt of downloaded messages and the at-least-once FetchMessagesAsync helper.
---

## ConfirmMessageAsync

```csharp
Task ConfirmMessageAsync(string messageId, ReceivedStatus status = ReceivedStatus.Complete, CancellationToken cancellationToken = default);
```

```csharp
public enum ReceivedStatus
{
    Partial,
    Complete,
}
```

Confirming a message tells the bank what to do next:

- `ReceivedStatus.Complete` (the default) tells the bank the message was received in full. **The
  message is then removed from the mailbox permanently** — the bank stops redelivering it, and it
  will not appear in a later `ListMessagesAsync` call again.
- `ReceivedStatus.Partial` tells the bank only part of the message was received; the bank keeps
  delivering it on subsequent calls.

```csharp
var message = await client.DownloadMessageAsync(messageId);
// ... process message.Content ...
await client.ConfirmMessageAsync(messageId, ReceivedStatus.Complete);
```

Because confirming as `Complete` is irreversible from the client's point of view, only confirm once
processing has genuinely finished — see [Production Checklist](/docs/guides/production-checklist) for
the operational implications.

## FetchMessagesAsync: List, Download and Confirm in One Sequence

```csharp
IAsyncEnumerable<CorporatePaymentsMessage> FetchMessagesAsync(OrderType? orderType = null, bool confirm = true, CancellationToken cancellationToken = default);
```

`FetchMessagesAsync` lists waiting messages, downloads them one at a time, and — when `confirm` is
`true` (the default) — confirms each one as `ReceivedStatus.Complete`. The important detail is
**when** that confirmation happens:

```csharp
await foreach (var message in client.FetchMessagesAsync())
{
    Process(message);
    // confirmation of THIS message happens here, on the next MoveNextAsync
}
```

Confirmation happens on the *next* call to `MoveNextAsync`, i.e. only after the loop body for the
current message has finished running. This gives at-least-once delivery semantics:

- If the consumer throws while processing a message, that message is never confirmed — the bank
  keeps redelivering it on the next call.
- If the enumeration is abandoned early (e.g. via `break`), the last yielded message is likewise left
  unconfirmed.
- A message is only ever confirmed after your code has had a chance to fully process it.

The trade-off is that a message can be delivered more than once if processing succeeded but something
between processing and the next iteration failed — your consumer should be idempotent with respect to
reprocessing the same message.

## MessageConfirmationException

If confirmation itself fails (a network error, or the gateway rejecting the confirmation request),
`FetchMessagesAsync` throws `MessageConfirmationException` from the following `MoveNextAsync` — not
silently swallowing the failure, and not losing the message that was already downloaded:

```csharp
public sealed class MessageConfirmationException : CommerzbankException
{
    public string MessageId { get; }
    public CorporatePaymentsMessage DownloadedMessage { get; }
}
```

```csharp
try
{
    await foreach (var message in client.FetchMessagesAsync())
        Process(message);
}
catch (MessageConfirmationException ex)
{
    // ex.DownloadedMessage was already downloaded and processed successfully —
    // only the confirmation call failed. Retry ConfirmMessageAsync(ex.MessageId), or
    // hand off ex.DownloadedMessage for manual follow-up instead of re-downloading it.
    await client.ConfirmMessageAsync(ex.MessageId);
}
```

When the exception that caused the confirmation failure is itself a `CommerzbankException`, its
`StatusCode`, `CorrelationId` and `RawResponse` are carried over onto the `MessageConfirmationException`.

## Next Steps

- [Downloading Messages](/docs/guides/downloading-messages)
- [Error Handling](/docs/guides/error-handling)
