---
title: Installation
category: Getting Started
order: 1
description: Install Commerzbank.NET and configure your project.
---

## NuGet Package

Install the Commerzbank.NET package via the .NET CLI:

```bash
dotnet add package Commerzbank.NET
```

Or via the NuGet Package Manager:

```powershell
Install-Package Commerzbank.NET
```

## Requirements

- .NET 10.0 or later
- A Commerzbank Corporate Payments API client ID and client secret (sandbox or production), issued via the Commerzbank developer portal

## Project Setup

Add the relevant namespaces to your project:

```csharp
using Commerzbank.NET;
using Commerzbank.NET.CorporatePayments;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Exceptions;
```

- `Commerzbank.NET` holds `CommerzbankOptions` and the dependency injection extensions.
- `Commerzbank.NET.CorporatePayments` holds `ICorporatePaymentsClient` and the order type, message and confirmation types.
- `Commerzbank.NET.CorporatePayments.Iso20022` holds the camt and pain.001/pain.008/pain.002 readers and writers.
- `Commerzbank.NET.Exceptions` holds the exception hierarchy.

## Next Steps

- [Quick Start](/docs/getting-started/quick-start) — Register the client and make your first call
- [Authentication](/docs/getting-started/authentication) — Understand the OAuth 2.0 client credentials flow
- [Configuration](/docs/getting-started/configuration) — Customize timeouts, certificates and the API environment
