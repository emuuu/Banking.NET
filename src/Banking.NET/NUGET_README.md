# Banking.NET

Banking.NET is a provider-neutral home for .NET banking API client libraries. The Commerzbank Corporate Payments API is the first (currently only) provider, available under the `Banking.NET.Commerzbank` namespace: OAuth client credentials, mutual TLS, EBICS order types, fragmented downloads and ISO 20022 (camt, pain) readers and writers. Unofficial, not affiliated with Commerzbank AG.

## Quick Start

```bash
dotnet add package Banking.NET
```

Register the client in your DI container:

```csharp
builder.Services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Sandbox;
}).AddCorporatePayments();
```

Or bind from configuration:

```csharp
builder.Services
    .AddCommerzbank(builder.Configuration.GetSection(CommerzbankOptions.SectionName))
    .AddCorporatePayments();
```

Inject and fetch waiting messages:

```csharp
public sealed class StatementService(ICorporatePaymentsClient client)
{
    public async Task PrintWaitingMessagesAsync()
    {
        await foreach (var message in client.FetchMessagesAsync())
            Console.WriteLine($"{message.MessageId}: {message.OrderType} ({message.Content.Length} bytes)");
    }
}
```

## Features

- **OAuth 2.0 client credentials** with automatic refresh-token reuse and re-authentication
- **Mutual TLS client certificates** (PKCS#12 or PEM) for production
- **`OrderType`** covers every documented EBICS order code, with room for customer-specific codes
- **Transparent multi-fragment downloads**, including streaming straight to a destination `Stream`
- **gzip-aware submission**, heartbeat, and at-least-once message confirmation (`FetchMessagesAsync`)
- **ISO 20022 readers** for camt.052/053/054 and pain.002
- **ISO 20022 writers** for pain.001 and pain.008, current and legacy schema versions
- **Typed exception hierarchy** carrying HTTP status, `X-CorrelationID` and the raw response body

## ISO 20022 Example

```csharp
var message = await client.DownloadMessageAsync(messageId);
var bankToCustomerMessage = CamtReader.Read(message);

foreach (var statement in bankToCustomerMessage.Statements)
    Console.WriteLine($"{statement.Account?.Iban}: {statement.Balances.Count} balance(s), {statement.Entries.Count} entrie(s)");
```

## Error Handling

```csharp
try
{
    var message = await client.DownloadMessageAsync(messageId);
}
catch (CommerzbankNotFoundException) { /* 404 */ }
catch (CommerzbankGoneException) { /* 410 */ }
catch (CommerzbankAuthenticationException) { /* token/401/400 with challenge */ }
catch (CommerzbankApiException ex) { /* other API errors, ex.CorrelationId */ }
```

## Documentation

For full documentation, guides, and an interactive sandbox playground visit the [project site](https://emuuu.github.io/Banking.NET/).

Source code and issue tracker: [GitHub](https://github.com/emuuu/Banking.NET)
