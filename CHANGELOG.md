# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.0] - Unreleased

### Added

**Authentication & Configuration**
- `AddCommerzbank(...)` / `AddCorporatePayments()` DI builder registering named, factory-managed `HttpClient`s for the API and token endpoints.
- OAuth 2.0 client credentials token provider (`ClientCredentialsTokenProvider`) reusing a refresh token while it is valid and falling back to a new client credentials request otherwise; `DelegateAccessTokenProvider` for an externally supplied token callback.
- Mutual TLS client certificates for production, loaded from a PKCS#12 or PEM file (`ClientCertificateLoader`) or supplied directly as an `X509Certificate2`.
- `CommerzbankOptions` validated on startup (`ValidateOnStart`), covering required credentials, mutually exclusive certificate settings, and the production certificate requirement.

**Corporate Payments Client**
- `ICorporatePaymentsClient`: heartbeat, list, download (with transparent multi-fragment reassembly and a streaming overload), confirm, and submit orders.
- `OrderType`: a value type covering every documented EBICS order code (camt.052/053/054/086 downloads, pain.001/002/008 uploads and status reports), with unknown/customer-specific codes still usable.
- `SubmitOrderAsync` overloads for `Stream`, `byte[]` and `string` content, with gzip detection independent of the `compress` flag.
- `FetchMessagesAsync`: an async-enumerable combining list, download and at-least-once confirmation, surfacing confirmation failures via `MessageConfirmationException` without losing the downloaded message.
- A typed exception hierarchy (`CommerzbankAuthenticationException`, `CommerzbankBadRequestException`, `CommerzbankNotFoundException`, `CommerzbankGoneException`, `CommerzbankApiException`) carrying HTTP status, `X-CorrelationID` and the raw response body.

**ISO 20022 — Reading**
- `CamtReader` for camt.052 (account report), camt.053 (account statement) and camt.054 (debit/credit notification), schema versions .02 and .08.
- `Camt086Reader` for camt.086 (bank services billing statement), covering the base structure.
- `Pain002Reader` for pain.002 (customer payment status report), schema versions .03 and .10.
- `Iso20022Document.Identify` to detect a document's message type and schema version from its XML namespace before choosing a reader.
- Namespace-agnostic parsing (matched by local element name); every model exposes the source `XElement` for fields not mapped to strongly typed properties.

**ISO 20022 — Writing**
- `Pain001Writer` for pain.001 (customer credit transfer initiation), schema versions .03 and .09.
- `Pain008Writer` for pain.008 (customer direct debit initiation), schema versions .02 and .08.
- Structural and SEPA-rulebook validation (`Iso20022ValidationException`) covering identifier and name lengths, IBAN/BIC format, positive amounts, and mandate requirements; optional SEPA character set validation via `Pain00xWriterOptions.ValidateCharacterSet`.

**Samples & Documentation**
- `Banking.NET.Samples.Console`: a sandbox walkthrough covering heartbeat, listing, downloading, camt/pain.002 inspection, message confirmation, and submitting a sample credit transfer.
