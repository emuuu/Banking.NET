using System.Net;
using Commerzbank.NET.CorporatePayments;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Exceptions;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Sandbox;

/// <summary>Verifies order submission (pain.001, pain.008) against the live sandbox. Never confirms messages.</summary>
public sealed class SandboxSubmitTests(SandboxFixture fixture, ITestOutputHelper output) : IClassFixture<SandboxFixture>
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctBytes_Sandbox_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, bytes);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, bytes): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctBytesCompressed_Sandbox_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, bytes, compress: true);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, bytes, compress=true): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctString_Sandbox_ReturnsCreated()
    {
        var xml = Pain001Writer.WriteToString(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, xml);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, string): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctStream_Sandbox_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);
        using var stream = new MemoryStream(bytes);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, stream);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, stream): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_StructurallyInvalidXml_Sandbox_LogsObservedResult()
    {
        // Deviation from the assumption that the gateway rejects non-ISO-20022 content: the sandbox
        // accepts a bare `<Document/>` root and returns 201, i.e. it does not validate the submitted
        // XML structurally against the order type's schema. Logged here for the report; if the
        // gateway ever does reject it, that is logged too rather than asserted away.
        try
        {
            var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, "<Document/>");
            output.WriteLine($"SubmitOrderAsync(structurally invalid XML): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
        }
        catch (CommerzbankException ex)
        {
            output.WriteLine($"SubmitOrderAsync(structurally invalid XML) rejected: exception={ex.GetType().Name}, status={(int?)ex.StatusCode}, correlationId present={!string.IsNullOrEmpty(ex.CorrelationId)}.");
        }
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_Cdd_Sandbox_LogsObservedResult()
    {
        var bytes = Pain008Writer.WriteToBytes(BuildDirectDebitInitiation(NewTestMessageId()), Pain008Version.V08);

        try
        {
            var result = await fixture.Client.SubmitOrderAsync(OrderType.CDD, bytes);
            output.WriteLine($"SubmitOrderAsync(CDD): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
            result.StatusCode.ShouldBe(HttpStatusCode.Created);
        }
        catch (CommerzbankException ex)
        {
            output.WriteLine($"SubmitOrderAsync(CDD) rejected: exception={ex.GetType().Name}, status={(int?)ex.StatusCode}, correlationId present={!string.IsNullOrEmpty(ex.CorrelationId)}.");
            ex.CorrelationId.ShouldNotBeNullOrEmpty();
        }
    }

    // "COMMERZBANK-NET-TEST-" (21 chars) + a 14-digit yyyyMMddHHmmss timestamp is exactly 35
    // characters, the ISO 20022 Max35Text limit enforced by PainValidator for these identifiers.
    private static string NewTestMessageId() => "COMMERZBANK-NET-TEST-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

    private static CreditTransferInitiation BuildCreditTransferInitiation(string messageId)
    {
        var timestamp = messageId[^14..];
        return new CreditTransferInitiation
        {
            MessageId = messageId,
            InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
            PaymentInformations =
            [
                new CreditTransferPaymentInformation
                {
                    PaymentInformationId = "PMTINF-" + timestamp,
                    RequestedExecutionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                    DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                    Transactions =
                    [
                        new CreditTransferTransaction
                        {
                            EndToEndId = "E2E-" + timestamp,
                            Amount = new Money(1.00m, "EUR"),
                            Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                            CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                        },
                    ],
                },
            ],
        };
    }

    private static DirectDebitInitiation BuildDirectDebitInitiation(string messageId)
    {
        var timestamp = messageId[^14..];
        return new DirectDebitInitiation
        {
            MessageId = messageId,
            InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
            PaymentInformations =
            [
                new DirectDebitPaymentInformation
                {
                    PaymentInformationId = "PMTINF-" + timestamp,
                    Scheme = DirectDebitScheme.Core,
                    SequenceType = SequenceType.Recurring,
                    RequestedCollectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                    CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                    CreditorSchemeId = "DE98ZZZ09999999999",
                    Transactions =
                    [
                        new DirectDebitTransaction
                        {
                            EndToEndId = "E2E-" + timestamp,
                            Amount = new Money(1.00m, "EUR"),
                            Mandate = new MandateInformation { MandateId = "MANDATE-" + timestamp, DateOfSignature = new DateOnly(2026, 1, 10) },
                            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                        },
                    ],
                },
            ],
        };
    }
}
