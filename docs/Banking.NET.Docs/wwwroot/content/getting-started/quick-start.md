---
title: Quick Start
category: Getting Started
order: 4
description: Register the client and make your first Corporate Payments API calls.
---

## Register the Client

```csharp
using Banking.NET.Commerzbank;
using Banking.NET.Commerzbank.CorporatePayments;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Sandbox;
})
.AddCorporatePayments();

var provider = services.BuildServiceProvider();
```

`AddCommerzbank` registers the OAuth infrastructure and the named HTTP clients;
`AddCorporatePayments` registers `ICorporatePaymentsClient` itself.

## Verify Connectivity

```csharp
var client = provider.GetRequiredService<ICorporatePaymentsClient>();

await client.HeartbeatAsync();
Console.WriteLine("Connected.");
```

`HeartbeatAsync` calls the API heartbeat endpoint and throws
`Banking.NET.Commerzbank.Exceptions.CommerzbankAuthenticationException` if the credentials are rejected.

## List Waiting Messages

```csharp
var messages = await client.ListMessagesAsync();

foreach (var info in messages)
    Console.WriteLine($"{info.MessageId} [{info.OrderType}] {info.Fragments} fragment(s), {info.Size} bytes");
```

`ListMessagesAsync` returns an empty list when the mailbox is empty (the gateway answers HTTP 204 in
that case). Pass an `OrderType` to restrict the result to a single order type, e.g.
`ListMessagesAsync(OrderType.C53)`.

## Download a Message

```csharp
if (messages.Count > 0)
{
    var message = await client.DownloadMessageAsync(messages[0]);

    Console.WriteLine($"Content-Type: {message.ContentType}, {message.FragmentCount} fragment(s)");
    Console.WriteLine(message.GetContentAsString());
}
```

`DownloadMessageAsync(MessageInfo, ...)` uses the fragment count already known from
`ListMessagesAsync`; an overload taking just the message ID is also available when you only have the
identifier. See [Downloading Messages](docs/guides/downloading-messages) for the streaming overload
and large-message considerations.

## Next Steps

- [Order Types](docs/guides/order-types) — The full set of download and upload order types
- [Downloading Messages](docs/guides/downloading-messages) — Fragments, streaming and large messages
- [Confirming Messages](docs/guides/confirming-messages) — Telling the bank a message was received
- [Submitting Orders](docs/guides/submitting-orders) — Uploading pain.001/pain.008 orders
- [Error Handling](docs/guides/error-handling) — The exception hierarchy
