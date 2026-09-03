---
title: Trying the API
category: Getting Started
order: 5
description: Why this docs site has no interactive sandbox demo, and how to exercise the client instead.
---

## Why Not in the Browser

As described in [Authentication](docs/getting-started/authentication), the Commerzbank sandbox
gateway and token endpoint do not return `Access-Control-Allow-Origin` for authenticated requests.
The sandbox answers an authenticated browser request with HTTP 403 and no CORS headers, and the
token response likewise carries no `Access-Control-Allow-Origin`; a browser blocks both regardless
of how the request was made, so a client running inside this docs site cannot call the sandbox. The
Corporate Payments API is reachable only from server-side or desktop applications.

## Run the Sample Console

[`Banking.NET.Samples.Console`](https://github.com/emuuu/Banking.NET/tree/main/samples/Banking.NET.Samples.Console)
walks through heartbeat, listing, downloading, camt/pain.002 inspection, message confirmation, and
submitting a sample credit transfer against the sandbox:

```
COMMERZBANK_SANDBOX_CLIENT_ID=... COMMERZBANK_SANDBOX_CLIENT_SECRET=... \
  dotnet run --project samples/Banking.NET.Samples.Console
```

Pass `--confirm` to also confirm downloaded messages, and `--submit-sample` to submit a sample
SEPA credit transfer; both are opt-in so the sample is safe to run repeatedly without either flag.
Sandbox credentials are issued through a project in the Commerzbank Developer Portal.

## Offline Tools

For working with ISO 20022 documents without any credentials or network access, this docs site
includes three offline tools that run entirely in the browser:

- [Parse camt / pain.002](tools/parse-iso20022) — paste a document and inspect the parsed structure
- [Build pain.001](tools/build-pain001) — fill in a form and generate a credit transfer initiation
- [Build pain.008](tools/build-pain008) — fill in a form and generate a direct debit initiation

## Next Steps

- [Quick Start](docs/getting-started/quick-start) — Register the client and make your first call
- [Order Types](docs/guides/order-types) — The full set of download and upload order types
- [Production Checklist](docs/guides/production-checklist) — Going live with mutual TLS
