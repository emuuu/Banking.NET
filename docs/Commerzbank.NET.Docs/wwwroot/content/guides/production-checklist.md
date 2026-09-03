---
title: Production Checklist
category: Guides
order: 9
description: What changes when moving a Commerzbank.NET integration from sandbox to production.
---

Sandbox and production behave differently in ways that are easy to miss during development. Work
through this list before switching `Environment` to `Production`.

## Certificate and Network

- **Obtain a signed client certificate.** Production requires mutual TLS; the sandbox does not.
  Submit a certificate signing request (CSR) to Commerzbank: RSA 4096, subject
  `O=Commerzbank AG`, `OU=01-37-95`, `CN=<your company name>`. You get back a private key and a
  Commerzbank-signed certificate. See [Authentication](/docs/getting-started/authentication).
- **Configure the certificate before switching environments.** Set `ClientCertificate` (an
  `X509Certificate2`) or `ClientCertificatePath` (+ `ClientCertificateKeyPath` for PEM) on
  `CommerzbankOptions`. Without one, `CommerzbankOptions.Validate()` throws on startup when
  `Environment == CommerzbankEnvironment.Production` and no `ApiBaseUrl` override is set.
- **Expect 403 without a certificate.** The production gateway answers HTTP 403 to any request
  missing a valid client certificate, independent of the bearer token — this looks nothing like an
  authentication failure and is easy to misdiagnose as a bad token if you are not aware of it.
- **The gateway only accepts IPv4.** Make sure the host(s) making outbound calls to the production
  gateway have IPv4 connectivity; an IPv6-only egress path will fail to connect.

## Configuration

- Set `Environment = CommerzbankEnvironment.Production` — this also switches the default API base
  URL and OAuth token endpoint (realm `external` instead of `sandbox`).
- Set a meaningful `ClientProduct` (e.g. `"MyErp/2.4"`). It has no functional effect on requests, but
  it is what lets Commerzbank identify which client and version is calling when investigating an
  issue.
- Double check `ClientId` / `ClientSecret` are the production credentials, not the sandbox ones —
  they come from a separate provisioning process (production projects are not listed in the developer
  portal; credentials arrive by other means).

## Message Confirmation

- **Confirm messages deliberately, never accidentally as `Complete`.** Confirming with
  `ReceivedStatus.Complete` removes the message from the mailbox permanently. Make sure that call
  only happens after your integration has durably persisted or fully processed the message content —
  see [Confirming Messages](/docs/guides/confirming-messages).
- **Handle `MessageConfirmationException`.** If `FetchMessagesAsync` fails to confirm a message after
  downloading it, the exception carries the message ID and the already-downloaded content
  (`DownloadedMessage`) precisely so that a confirmation failure does not turn into silently dropped
  data. Make sure this exception is caught and acted on (retry the confirmation, or route the message
  for manual follow-up) rather than allowed to crash an unattended process.

## Before You Flip the Switch

- **Test thoroughly against the sandbox first.** Exercise the full lifecycle — listing, downloading
  (including multi-fragment messages), confirming, and submitting both pain.001 and pain.008 orders —
  against the sandbox, where mistakes cost nothing and mock data is safe to experiment with.
- Verify your error handling paths against the sandbox's documented quirks (see
  [Error Handling](/docs/guides/error-handling)), since production is expected to exhibit the same
  gateway behavior for status codes and headers.
- Confirm your process handles the retry-once behavior of `CommerzbankAuthHandler` correctly, i.e.
  that a call only fails after two attempts with two different tokens, not on the first rejected one.

## Next Steps

- [Authentication](/docs/getting-started/authentication)
- [Configuration](/docs/getting-started/configuration)
- [Error Handling](/docs/guides/error-handling)
