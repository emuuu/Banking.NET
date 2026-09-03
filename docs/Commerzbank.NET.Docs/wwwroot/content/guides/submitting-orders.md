---
title: Submitting Orders
category: Guides
order: 4
description: Uploading pain.001 and pain.008 orders with SubmitOrderAsync.
---

## SubmitOrderAsync Overloads

`ICorporatePaymentsClient` exposes three overloads for submitting an order, differing only in how the
content is supplied:

```csharp
Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, Stream content, bool compress = false, CancellationToken cancellationToken = default);
Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, byte[] content, bool compress = false, CancellationToken cancellationToken = default);
Task<OrderSubmissionResult> SubmitOrderAsync(OrderType orderType, string xml, bool compress = false, CancellationToken cancellationToken = default);
```

The `string` overload encodes `xml` as UTF-8 without a byte order mark before submitting it. All
three ultimately submit the same way — pick whichever shape matches what you already have (a written
document, its bytes, or a stream).

## The Order Type Must Be an Upload Type

```csharp
var xml = Pain001Writer.WriteToString(initiation, Pain001Version.V09);
var result = await client.SubmitOrderAsync(OrderType.CCT, xml);
```

Passing a download order type (e.g. `OrderType.C53`) throws `ArgumentException` — see
[Order Types](/docs/guides/order-types) for the full list of upload types (`OrderType.UploadTypes`).
For the `Stream` overload, this validation happens *before* the stream is read.

## Compression

```csharp
var result = await client.SubmitOrderAsync(OrderType.CCT, xml, compress: true);
```

Set `compress: true` to gzip-compress the content before submitting it; the client sets
`Content-Type: application/gzip` accordingly. If the content already starts with the gzip magic bytes
(`0x1F 0x8B`) — for the `byte[]` and `Stream` overloads — it is submitted as-is with
`Content-Type: application/gzip`, regardless of the `compress` argument; it is never compressed twice.
With `compress: false` (the default) and non-gzip content, the client sets
`Content-Type: application/xml`.

## OrderSubmissionResult

```csharp
public sealed record OrderSubmissionResult(HttpStatusCode StatusCode, Uri? Location, string? RawResponse);
```

`StatusCode` is typically 201 Created. In sandbox testing, the gateway has been observed to answer
201 Created **without** a `Location` header and with an **empty body** — so `Location` and
`RawResponse` are regularly `null` even on a successful submission. Do not rely on either being
populated; treat a non-exception return from `SubmitOrderAsync` as the success signal.

```csharp
var result = await client.SubmitOrderAsync(OrderType.CCT, xml);
Console.WriteLine($"Submitted: {result.StatusCode}");
if (result.Location is { } location)
    Console.WriteLine($"Location: {location}");
```

A rejected submission (e.g. an invalid `OrderType` header, or content the gateway cannot parse)
throws `CommerzbankBadRequestException` — see [Error Handling](/docs/guides/error-handling).

## Next Steps

- [ISO 20022: Writing pain.001 / pain.008](/docs/guides/iso20022-writing-pain) — Building the content to submit
- [Order Types](/docs/guides/order-types)
- [Error Handling](/docs/guides/error-handling)
