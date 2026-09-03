---
title: Authentication
category: Getting Started
order: 2
description: OAuth 2.0 client credentials against the Commerzbank sandbox and production gateways.
---

## OAuth 2.0 Client Credentials

The Corporate Payments API authenticates with the OAuth 2.0 client credentials grant against a
Keycloak realm. There are two realms, one per environment:

| Environment | Token endpoint |
|---|---|
| Sandbox | `https://api-sandbox.commerzbank.com/auth/realms/sandbox/protocol/openid-connect/token` |
| Production | `https://api.commerzbank.com/auth/realms/external/protocol/openid-connect/token` |

`CommerzbankOptions.GetTokenEndpointUri()` picks the right one based on `Environment`, unless
`TokenEndpoint` overrides it. The request is a form-urlencoded POST:

```
grant_type=client_credentials&client_id=...&client_secret=...
```

Configure the client ID and secret through `AddCommerzbank`:

```csharp
using Banking.NET.Commerzbank;

services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Sandbox;
})
.AddCorporatePayments();
```

Never commit real client IDs or secrets. Load them from configuration, environment variables or a
secret store, and keep placeholder values such as `your-client-id` / `your-client-secret` out of
version control once replaced with real ones.

## Token Lifecycle

`ClientCredentialsTokenProvider` requests and caches the access token, and reuses the Keycloak
refresh token to renew it (`grant_type=refresh_token`) as long as it is still valid, falling back to
a new client credentials request otherwise. This all happens transparently — application code only
ever sees `ICorporatePaymentsClient` calls succeed or fail.

To supply tokens from an external source instead (e.g. a shared token cache across services), set
`CommerzbankOptions.AccessTokenProvider` — see [Configuration](/docs/getting-started/configuration).

## How the Bearer Token Is Attached

`CommerzbankAuthHandler` is registered as a message handler on the Corporate Payments HTTP client.
On every request it:

1. Obtains the current access token from `IAccessTokenProvider` and sets the `Authorization: Bearer`
   header.
2. Sends the request.
3. If the gateway rejects it — HTTP 401, or HTTP 400 carrying a `WWW-Authenticate` challenge with
   `error="invalid_token"` — it invalidates the cached token, obtains a fresh one, and retries the
   request **exactly once** with the new token. Whatever that retry returns is what the caller sees;
   there is no further retry loop.

No other status codes trigger a retry. Rate limiting or transient server errors are the caller's
responsibility to handle (e.g. via a Polly policy registered on the named HTTP client).

## Sandbox: No CORS Preflight on the Token Request

The sandbox token endpoint answers a form-urlencoded POST as a CORS "simple request" — no
`OPTIONS` preflight is required, and the response carries `Access-Control-Allow-Origin` for the
calling origin. This means a browser can call the sandbox token endpoint directly, which is what the
docs site playground relies on.

The gateway itself (`/heartbeat`, `/messages`, ...) does support full CORS preflighting, but calling
it without a valid token returns HTTP 400 (not 401) with a `WWW-Authenticate` challenge — see
[Error Handling](/docs/guides/error-handling).

## Production: Mutual TLS Required

Sandbox authentication needs nothing beyond the client ID and secret. Production additionally
requires a client certificate for mutual TLS — the gateway answers **403** to any request without
one, regardless of the bearer token.

The certificate is obtained through a certificate signing request (CSR) process with Commerzbank:

- Key: RSA 4096
- Subject: `O=Commerzbank AG`, `OU=01-37-95`, `CN=<your company name>`
- Result: a private key and a Commerzbank-signed certificate

Configure the certificate on `CommerzbankOptions` (object, PKCS#12 file, or PEM file pair) and set
`Environment = CommerzbankEnvironment.Production`. See
[Configuration](/docs/getting-started/configuration) for the exact properties and
[Production Checklist](/docs/guides/production-checklist) for the full go-live checklist.

## Next Steps

- [Configuration](/docs/getting-started/configuration) — All `CommerzbankOptions` properties
- [Quick Start](/docs/getting-started/quick-start) — Register the client and make your first call
- [Error Handling](/docs/guides/error-handling) — How authentication failures surface as exceptions
