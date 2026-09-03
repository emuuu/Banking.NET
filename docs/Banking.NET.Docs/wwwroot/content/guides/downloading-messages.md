---
title: Downloading Messages
category: Guides
order: 2
description: Listing, downloading and streaming Corporate Payments messages and their fragments.
---

## Listing Waiting Messages

```csharp
var messages = await client.ListMessagesAsync();
// or restrict to a single order type:
var statements = await client.ListMessagesAsync(OrderType.C53);
```

`ListMessagesAsync` returns `IReadOnlyList<MessageInfo>`, one entry per waiting message:

```csharp
public sealed record MessageInfo(string MessageId, OrderType OrderType, int Fragments, long Size);
```

An empty mailbox (or an order type with nothing waiting) returns an empty list.

## Downloading a Whole Message

Two overloads download a complete message with all fragments concatenated into memory:

```csharp
Task<CorporatePaymentsMessage> DownloadMessageAsync(string messageId, CancellationToken cancellationToken = default);
Task<CorporatePaymentsMessage> DownloadMessageAsync(MessageInfo message, CancellationToken cancellationToken = default);
```

Use the `MessageInfo` overload whenever you already have one from `ListMessagesAsync` — its known
`Fragments` count lets the client download in fewer round trips and lets the returned
`CorporatePaymentsMessage.OrderType` be populated. The `string messageId` overload is for when you
only have the identifier (e.g. it was stored from an earlier run); the fragment count is unknown, so
the client probes the gateway to detect how it splits the message.

```csharp
public sealed class CorporatePaymentsMessage
{
    public required string MessageId { get; init; }
    public OrderType? OrderType { get; init; }        // set only when downloaded via MessageInfo
    public required byte[] Content { get; init; }
    public string? ContentType { get; init; }
    public int FragmentCount { get; init; }
    public bool IsGzip { get; }                       // true if Content starts with 0x1F 0x8B

    public Stream OpenContentStream(bool decompress = true);
    public string GetContentAsString();
}
```

`OpenContentStream` returns a `MemoryStream` over the raw bytes, or transparently wraps it in a
`GZipStream` when `IsGzip` is true and `decompress` is not set to `false`. `GetContentAsString`
decompresses first if needed and detects the text encoding from a byte order mark, falling back to
UTF-8.

## Large Messages: Prefer Streaming

The sandbox mailbox has been observed to contain a three-fragment camt.053 message of roughly 21.8 MB.
Buffering a message that size as a `byte[]` and then again as a `string` (via `GetContentAsString`)
doubles the memory footprint for no benefit if you are about to hand the content to `CamtReader`
anyway. For large messages, either:

- use the streaming overload below, or
- check `Content.Length` (or `MessageInfo.Size` before downloading) and switch to streaming past a
  size threshold that makes sense for your process.

```csharp
Task<int> DownloadMessageAsync(string messageId, Stream destination, int? fragmentCount = null, CancellationToken cancellationToken = default);
```

This overload streams every fragment directly into `destination` using
`HttpCompletionOption.ResponseHeadersRead` and `CopyToAsync`, without ever buffering the whole message
as a byte array. Pass the known fragment count (e.g. `messageInfo.Fragments`) to skip the probing
step; it returns the number of fragments that were read.

```csharp
await using var file = File.Create("statement.xml");
var fragmentCount = await client.DownloadMessageAsync(messageInfo.MessageId, file, messageInfo.Fragments);
```

`CamtReader.Read(Stream)` and `Pain002Reader.Read(Stream)` both accept a stream directly, so you can
feed a downloaded, decompressed stream straight into the reader without an intermediate string.

## Low-Level: Downloading a Single Fragment

```csharp
Task<MessageFragment> DownloadFragmentAsync(string messageId, int fragment, CancellationToken cancellationToken = default);
```

```csharp
public sealed record MessageFragment(int Index, byte[] Content, string? ContentType, bool IsPartial);
```

This is the building block behind `DownloadMessageAsync` and is rarely needed directly. `fragment` is
the zero-based fragment index; `IsPartial` is `true` when the gateway answered with HTTP 206
(more fragments follow) rather than 200 OK. Requesting a fragment index the gateway cannot deliver
(HTTP 416) surfaces as a `CommerzbankApiException`.

## Next Steps

- [Confirming Messages](docs/guides/confirming-messages) — Telling the bank a message was received
- [ISO 20022: Reading camt Messages](docs/guides/iso20022-reading-camt)
- [ISO 20022: Reading pain.002 Messages](docs/guides/iso20022-reading-pain002)
