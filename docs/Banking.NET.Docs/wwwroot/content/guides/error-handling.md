---
title: Error Handling
category: Guides
order: 8
description: The exception hierarchy and how Commerzbank gateway responses map onto it.
---

## Exception Hierarchy

Banking.NET uses a typed exception hierarchy for API errors:

```
CommerzbankException (base)
  ├── CommerzbankApiException                 (any other unsuccessful status)
  ├── CommerzbankAuthenticationException       (token endpoint failure, 401, or 400 with WWW-Authenticate)
  ├── CommerzbankBadRequestException           (400 without WWW-Authenticate)
  ├── CommerzbankNotFoundException             (404)
  └── CommerzbankGoneException                 (410)

Banking.NET.Commerzbank.CorporatePayments.MessageConfirmationException           : CommerzbankException
Banking.NET.Commerzbank.CorporatePayments.Iso20022.Iso20022ValidationException   : CommerzbankException
```

## CommerzbankException

All exceptions inherit from `CommerzbankException`, which carries the HTTP response context:

```csharp
public class CommerzbankException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? CorrelationId { get; }
    public string? RawResponse { get; }
}
```

`CorrelationId` is populated from the `X-CorrelationID` response header whenever the gateway sends
one — which it does even on error responses. It is always worth logging alongside the exception
message; it is the identifier Commerzbank support needs to look up a specific failed request.

## Handling Errors

```csharp
try
{
    var result = await client.SubmitOrderAsync(OrderType.CCT, xml);
}
catch (CommerzbankNotFoundException)
{
    Console.WriteLine("The referenced message no longer exists.");
}
catch (CommerzbankAuthenticationException ex)
{
    Console.WriteLine($"Authentication failed ({ex.CorrelationId}): {ex.Message}");
}
catch (CommerzbankBadRequestException ex)
{
    Console.WriteLine($"Request rejected: {ex.Message}");
}
catch (CommerzbankApiException ex)
{
    Console.WriteLine($"API error ({ex.StatusCode}, {ex.CorrelationId}): {ex.Message}");
}
catch (CommerzbankException ex)
{
    Console.WriteLine($"General error: {ex.Message}");
}
```

## The Sandbox Answers Missing Tokens with 400, Not 401

A detail worth designing for explicitly: the sandbox gateway answers a request with a missing or
invalid bearer token with **HTTP 400**, not 401, carrying a `WWW-Authenticate` challenge such as:

```
WWW-Authenticate: Bearer realm="DefaultRealm", error="invalid_request", error_description="Unable to find token in the message"
```

Banking.NET accounts for this in its error mapping: a 400 response carrying a `WWW-Authenticate`
challenge (any `error` value) is mapped to `CommerzbankAuthenticationException`, exactly like a 401
would be. A 400 *without* such a challenge is mapped to `CommerzbankBadRequestException` instead —
so catching `CommerzbankAuthenticationException` reliably catches authentication problems regardless
of which of the two status codes the gateway happened to use.

This exception mapping is a separate, slightly broader condition than what makes
`CommerzbankAuthHandler` retry the request once with a fresh token: the handler only retries on a
plain 401, or a 400 whose `WWW-Authenticate` challenge specifically carries `error="invalid_token"`
— a 400 with a *different* challenge (e.g. `invalid_client`) still surfaces as
`CommerzbankAuthenticationException` but is not retried, since a fresh token from the same
credentials would fail the same way. See
[Authentication](docs/getting-started/authentication) for the retry mechanics.

## Iso20022ValidationException

Thrown by the camt/pain.002 readers and the pain.001/pain.008 writers for *structural* or content
problems in an otherwise well-formed ISO 20022 document — a missing root element, a missing required
element behind a non-nullable property, or (for the writers) a validation rule violation such as an
amount that is not greater than zero or an invalid IBAN. XML that is not even well-formed (e.g. an
unclosed tag) never reaches this point: `Iso20022Document.Load` parses it with `XDocument.Load` and
lets a plain `System.Xml.XmlException` propagate uncaught — catch that separately if the input may
not be well-formed XML at all.

```csharp
public sealed class Iso20022ValidationException : CommerzbankException
{
    public string? Path { get; }
}
```

`Path` names the offending element or property — an XML element path while reading (e.g.
`"Ntry/Amt"`), or a C#-style navigation path while writing (e.g.
`"PaymentInformations[0].Transactions[2].EndToEndId"`). See
[ISO 20022: Reading camt Messages](docs/guides/iso20022-reading-camt) and
[ISO 20022: Writing pain.001 / pain.008](docs/guides/iso20022-writing-pain) for details.

## MessageConfirmationException

Thrown by `FetchMessagesAsync` when a message was downloaded successfully but confirming it failed:

```csharp
public sealed class MessageConfirmationException : CommerzbankException
{
    public string MessageId { get; }
    public CorporatePaymentsMessage DownloadedMessage { get; }
}
```

`DownloadedMessage` means a confirmation failure never loses the content that was already
successfully retrieved. See [Confirming Messages](docs/guides/confirming-messages) for the full
at-least-once semantics this is part of.

## Common Error Scenarios

| Exception | Typical HTTP status | Common causes |
|---|---|---|
| `CommerzbankAuthenticationException` | 401, or 400 with `WWW-Authenticate` | Missing, expired or invalid bearer token; invalid client credentials at the token endpoint |
| `CommerzbankBadRequestException` | 400 (no challenge) | Invalid `OrderType` header, malformed request body |
| `CommerzbankNotFoundException` | 404 | No message with the given ID exists |
| `CommerzbankGoneException` | 410 | The message existed but is no longer available for download |
| `CommerzbankApiException` | Other non-success statuses | Unexpected fragment sequence, fragment count exceeded, server errors |
| `Iso20022ValidationException` | — (local) | Structurally invalid ISO 20022 document, or a writer validation rule violation |
| `System.Xml.XmlException` | — (local) | The input is not even well-formed XML |
| `MessageConfirmationException` | — (local, wraps another exception) | `ConfirmMessageAsync` failed after a message was downloaded via `FetchMessagesAsync` |

## Next Steps

- [Authentication](docs/getting-started/authentication)
- [Production Checklist](docs/guides/production-checklist)
