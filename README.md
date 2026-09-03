<p align="center">
  <img src="https://raw.githubusercontent.com/emuuu/Banking.NET/main/icon.png" alt="Banking.NET" width="128" />
</p>

<h1 align="center">Banking.NET</h1>

Banking.NET is a provider-neutral home for .NET banking API client libraries. The Commerzbank Corporate Payments API is the first (currently only) provider, available under the `Banking.NET.Commerzbank` namespace: OAuth client credentials, mutual TLS, EBICS order types, fragmented downloads and ISO 20022 (camt, pain) readers and writers. This is an unofficial, community-maintained library and is not affiliated with or endorsed by Commerzbank AG.

[![NuGet](https://img.shields.io/nuget/v/Banking.NET.svg)](https://www.nuget.org/packages/Banking.NET)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Banking.NET.svg)](https://www.nuget.org/packages/Banking.NET)
[![CI](https://github.com/emuuu/Banking.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/emuuu/Banking.NET/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docs](https://img.shields.io/badge/Docs-GitHub%20Pages-blue)](https://emuuu.github.io/Banking.NET/)

**Feature highlights:**

- OAuth 2.0 client credentials with automatic refresh-token reuse and re-authentication
- Mutual TLS client certificates (PKCS#12 or PEM) for production
- `OrderType` covers every documented EBICS order code, with room for customer-specific codes the bank enables outside the public list
- Transparent multi-fragment downloads, including streaming straight to a destination `Stream`
- gzip-aware order submission, heartbeat, and at-least-once message confirmation (`FetchMessagesAsync`)
- ISO 20022 readers for camt.052/053/054 (account reports, statements, notifications) and pain.002 (payment status reports)
- ISO 20022 writers for pain.001 (credit transfers) and pain.008 (direct debits), in both current and legacy schema versions
- Typed exception hierarchy carrying HTTP status, `X-CorrelationID` and the raw response body

## Prerequisites

- A project in the [Commerzbank Developer Portal](https://developer.commerzbank.com/) with the Corporate Payments API enabled, and sandbox client credentials
- For production: a signed agreement with Commerzbank and a client certificate issued through the portal's CSR process
- .NET 10.0

## Installation

```bash
dotnet add package Banking.NET
```

## Getting Started

### 1. Register services (sandbox)

```csharp
builder.Services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Sandbox;
}).AddCorporatePayments();
```

### 2. Configuration via appsettings.json

```json
{
  "Commerzbank": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "Sandbox"
  }
}
```

```csharp
builder.Services
    .AddCommerzbank(builder.Configuration.GetSection(CommerzbankOptions.SectionName))
    .AddCorporatePayments();
```

### 3. Inject and use the client

```csharp
public sealed class StatementService(ICorporatePaymentsClient client)
{
    public async Task PrintWaitingMessagesAsync()
    {
        await foreach (var message in client.FetchMessagesAsync())
        {
            Console.WriteLine($"{message.MessageId}: {message.OrderType} ({message.Content.Length} bytes)");
        }
    }
}
```

## Production

Production requires a client certificate issued by Commerzbank; the gateway does not accept plain client credentials there. Request the certificate through the [Developer Portal](https://developer.commerzbank.com/)'s CSR process (RSA 4096) and configure it alongside the same client credentials:

```csharp
builder.Services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Production;
    options.ClientCertificatePath = "/path/to/client.pfx";
    options.ClientCertificatePassword = "your-certificate-password";
}).AddCorporatePayments();
```

The production gateway is reachable over IPv4 only.

## Order Types

`OrderType` exposes a static field for every documented code. Availability depends on the agreement with the bank; unrecognized codes still work (`OrderType.Parse("XYZ")`), they just report `IsKnown == false`.

| Code | Direction | Message | Schema versions | Description |
|---|---|---|---|---|
| `C52` | Download | camt.052 | .001.08, .001.02 | Intraday account report |
| `C53` | Download | camt.053 | .001.08, .001.02 | Account statement |
| `C54` | Download | camt.054 | .001.08 | Debit and credit notification |
| `C86` | Download | camt.086 | .001.02, .001.01 | Bank services billing statement |
| `HAC` | Download | pain.002 | .001.10, .001.03 | Customer acknowledgement (payment status report) |
| `AXS` | Download | pain.002 | .001.10 | Payment status report for cross-border credit transfers |
| `CUZ` | Download | pain.002 | .001.10 | Payment status report for urgent credit transfers |
| `XIP` | Download | pain.002 | .001.03, .001.10 | Payment status report for XIC and XID orders |
| `CIZ` | Download | pain.002 | .001.10 | Payment status report for instant credit transfers |
| `CDZ` | Download | pain.002 | .001.10 | Payment status report for direct debits |
| `CRZ` | Download | pain.002 | .001.10 | Payment status report for credit transfers |
| `CCT` | Upload | pain.001 | .001.09 | SEPA credit transfer |
| `CTV` | Upload | pain.001 | .001.09 | SEPA credit transfer (CTV) |
| `CIP` | Upload | pain.001 | .001.09 | SEPA instant credit transfer |
| `CIV` | Upload | pain.001 | .001.09 | SEPA instant credit transfer (CIV) |
| `XIC` | Upload | pain.001 | .001.03 | SEPA credit transfer (pain.001.001.03) |
| `CDD` | Upload | pain.008 | .001.08 | SEPA direct debit (CORE) |
| `CDB` | Upload | pain.008 | .001.08 | SEPA direct debit (B2B) |
| `XID` | Upload | pain.008 | .001.02 | SEPA direct debit (pain.008.001.02) |
| `AXZ` | Upload | pain.001 | .001.09 | Cross-border credit transfer |
| `CCU` | Upload | pain.001 | .001.09 | Urgent credit transfer |

## Downloading & Fragments

The gateway may split large messages into fragments. `DownloadMessageAsync` retrieves and concatenates all of them automatically:

```csharp
var message = await client.DownloadMessageAsync(messageId);
Console.WriteLine(message.GetContentAsString());
```

For large messages (a camt.053 statement can run into tens of megabytes), stream fragments directly to a destination instead of buffering them in memory:

```csharp
await using var destination = File.Create("statement.xml");
var fragmentCount = await client.DownloadMessageAsync(messageId, destination);
```

## Confirming

The bank keeps redelivering a message until it is confirmed. `FetchMessagesAsync` confirms each message only after it has been yielded to the caller, so an exception while processing — or abandoning the enumeration early — leaves that message unconfirmed and it comes back on the next call:

```csharp
await foreach (var message in client.FetchMessagesAsync(OrderType.C53))
{
    ProcessStatement(message);
    // Confirmed automatically before the next message is yielded.
}
```

If confirmation itself fails, the already-downloaded message is not lost — it is attached to the thrown `MessageConfirmationException`. Confirmation can also be done manually, e.g. after only partially reading a message:

```csharp
var message = await client.DownloadMessageAsync(messageId);
await client.ConfirmMessageAsync(messageId, ReceivedStatus.Partial);
```

## Submitting Orders

```csharp
var result = await client.SubmitOrderAsync(OrderType.CCT, xml, compress: true);
Console.WriteLine(result.StatusCode);
```

Content starting with the gzip magic bytes is submitted as-is regardless of `compress`; otherwise `compress: true` gzip-encodes the content before sending it.

## ISO 20022

### Reading a camt.053 statement

```csharp
var message = await client.DownloadMessageAsync(messageId);
var bankToCustomerMessage = CamtReader.Read(message);

foreach (var statement in bankToCustomerMessage.Statements)
{
    Console.WriteLine($"{statement.Account?.Iban}: {statement.Balances.Count} balance(s), {statement.Entries.Count} entrie(s)");
}
```

### Reading a pain.002 payment status report

```csharp
var message = await client.DownloadMessageAsync(messageId);
var report = Pain002Reader.Read(message);

Console.WriteLine(report.OriginalGroup.GroupStatus);
```

### Writing a pain.001 credit transfer

```csharp
var initiation = new CreditTransferInitiation
{
    MessageId = "MSGID-0001",
    InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
    PaymentInformations =
    [
        new CreditTransferPaymentInformation
        {
            PaymentInformationId = "PMTINF-0001",
            RequestedExecutionDate = new DateOnly(2026, 9, 15),
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
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
await client.SubmitOrderAsync(OrderType.CCT, xml);
```

### Writing a pain.008 direct debit

```csharp
var initiation = new DirectDebitInitiation
{
    MessageId = "MSGID-0002",
    InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
    PaymentInformations =
    [
        new DirectDebitPaymentInformation
        {
            PaymentInformationId = "PMTINF-0001",
            Scheme = DirectDebitScheme.Core,
            SequenceType = SequenceType.Recurring,
            RequestedCollectionDate = new DateOnly(2026, 9, 15),
            Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
            CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
            CreditorSchemeId = "DE98ZZZ09999999999",
            Transactions =
            [
                new DirectDebitTransaction
                {
                    EndToEndId = "E2E-0001",
                    Amount = new Money(100.00m, "EUR"),
                    Mandate = new MandateInformation { MandateId = "MANDATE-0001", DateOfSignature = new DateOnly(2026, 1, 10) },
                    Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                    DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                },
            ],
        },
    ],
};

var xml = Pain008Writer.WriteToString(initiation, Pain008Version.V08);
await client.SubmitOrderAsync(OrderType.CDD, xml);
```

Every reader and writer also accepts raw XML (`string`/`Stream`/`XDocument`) directly, independent of the Corporate Payments client — useful for testing or offline processing.

## Error Handling

```csharp
try
{
    var message = await client.DownloadMessageAsync(messageId);
}
catch (CommerzbankNotFoundException)
{
    // 404 — no message with that ID
}
catch (CommerzbankGoneException)
{
    // 410 — the message existed but is no longer available for download
}
catch (CommerzbankAuthenticationException)
{
    // Token endpoint failure, 401, or 400 with a WWW-Authenticate challenge
}
catch (CommerzbankApiException ex)
{
    // Any other unsuccessful status
    Console.WriteLine($"{ex.StatusCode}: {ex.Message} (correlation {ex.CorrelationId})");
}
```

| Exception | HTTP Status | When |
|---|---|---|
| `CommerzbankAuthenticationException` | 401, or 400 with a challenge | Token endpoint error or unauthenticated request |
| `CommerzbankBadRequestException` | 400 without a challenge | Rejected request, e.g. an invalid `OrderType` |
| `CommerzbankNotFoundException` | 404 | Resource not found |
| `CommerzbankGoneException` | 410 | Message no longer available |
| `CommerzbankApiException` | Various | Other unsuccessful gateway responses |
| `CommerzbankException` | — | Base exception; carries `StatusCode`, `CorrelationId` and `RawResponse` where available |

`MessageConfirmationException` (thrown by `FetchMessagesAsync`) and `Iso20022ValidationException` (thrown by the readers and writers) also derive from `CommerzbankException`.

## Docs & Playground

The [documentation site](https://emuuu.github.io/Banking.NET/) is generated from the library's XML docs and includes an interactive playground. The playground talks to the sandbox only — production requires mutual TLS, which a browser cannot do — and client credentials entered there stay in the browser's session storage; they are never sent anywhere but the Commerzbank sandbox.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, coding conventions and pull request guidelines.

## License

MIT
