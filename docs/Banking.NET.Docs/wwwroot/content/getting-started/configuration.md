---
title: Configuration
category: Getting Started
order: 3
description: CommerzbankOptions properties and both AddCommerzbank registration styles.
---

## CommerzbankOptions

All client behavior is configured through `CommerzbankOptions`:

| Property | Description |
|---|---|
| `ClientId` | The OAuth client ID. Required unless `AccessTokenProvider` is set. |
| `ClientSecret` | The OAuth client secret. Required unless `AccessTokenProvider` is set. |
| `Environment` | `CommerzbankEnvironment.Sandbox` (default) or `CommerzbankEnvironment.Production`. |
| `ApiBaseUrl` | Absolute URL overriding the environment host, e.g. a reverse proxy or a test server. Any path is kept as a prefix — do not include the API path itself, the client appends it. |
| `TokenEndpoint` | Absolute URL overriding the OAuth token endpoint. |
| `ClientCertificate` | An `X509Certificate2` to present for mutual TLS. Mutually exclusive with `ClientCertificatePath`. |
| `ClientCertificatePath` | Path to a PKCS#12 (`.pfx`/`.p12`) or PEM certificate file. Mutually exclusive with `ClientCertificate`. |
| `ClientCertificateKeyPath` | Path to a PEM private key file; only used together with a PEM certificate in `ClientCertificatePath`. |
| `ClientCertificatePassword` | Password protecting `ClientCertificatePath` or `ClientCertificateKeyPath`, if any. |
| `AccessTokenProvider` | `Func<CancellationToken, ValueTask<string>>`. When set, `ClientId`/`ClientSecret` are not required and the library never requests a token itself. |
| `ClientProduct` | Sent as the `ClientProduct` header on every API request when set (e.g. `"MyErp/2.4"`). |
| `Timeout` | Timeout for Corporate Payments API requests. Defaults to 100 seconds. |
| `TokenTimeout` | Timeout for OAuth token requests. Defaults to 30 seconds. |
| `UseRefreshToken` | Whether to use a refresh token to renew an expiring access token before falling back to a new client credentials request. Defaults to `true`. |
| `TokenExpiryMargin` | Safety margin subtracted from a token's expiry when deciding whether it is still usable. Defaults to 30 seconds. |

Production requires a client certificate (`ClientCertificate` or `ClientCertificatePath`) unless
`ApiBaseUrl` overrides the gateway with a proxy that does not require mutual TLS. Options are
validated on startup (`ValidateOnStart()`); an invalid combination throws with a message naming the
offending property.

## Registering via a Delegate

```csharp
using Banking.NET.Commerzbank;
using Banking.NET.Commerzbank.CorporatePayments;

services.AddCommerzbank(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.Environment = CommerzbankEnvironment.Sandbox;
    options.ClientProduct = "MyErp/2.4";
})
.AddCorporatePayments();
```

## Registering via IConfiguration

`AddCommerzbank` also accepts an `IConfiguration` section, bound directly onto `CommerzbankOptions`.
The section name is available as `CommerzbankOptions.SectionName` (`"Commerzbank"`):

```csharp
using Banking.NET.Commerzbank;
using Banking.NET.Commerzbank.CorporatePayments;

services.AddCommerzbank(configuration.GetSection(CommerzbankOptions.SectionName))
    .AddCorporatePayments();
```

```json
{
  "Commerzbank": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "Sandbox",
    "ClientProduct": "MyErp/2.4",
    "Timeout": "00:01:40",
    "TokenTimeout": "00:00:30"
  }
}
```

For production, add the certificate path instead of embedding a certificate object in configuration:

```json
{
  "Commerzbank": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "Production",
    "ClientCertificatePath": "/etc/commerzbank/client.p12",
    "ClientCertificatePassword": "your-certificate-password"
  }
}
```

Keep `appsettings.*.json` files carrying real credentials or certificate paths out of source control.

## External Token Sources

Set `AccessTokenProvider` when tokens are obtained elsewhere, e.g. from a shared cache across
services or a custom identity provider integration:

```csharp
services.AddCommerzbank(options =>
{
    options.AccessTokenProvider = async cancellationToken =>
        await myTokenCache.GetTokenAsync(cancellationToken);
})
.AddCorporatePayments();
```

With `AccessTokenProvider` set, `ClientId` and `ClientSecret` are not required and the library never
makes a token request on its own — every call to `IAccessTokenProvider.GetAccessTokenAsync` is
forwarded to the delegate.

## Next Steps

- [Authentication](/docs/getting-started/authentication) — Token lifecycle and mutual TLS details
- [Quick Start](/docs/getting-started/quick-start) — Register the client and make your first call
