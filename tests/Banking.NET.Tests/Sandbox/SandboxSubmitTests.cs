using System.Net;
using Banking.NET.Commerzbank.CorporatePayments;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>Verifies order submission (pain.001, pain.008) against the live sandbox. Each submission leaves a mock order behind in the sandbox — not a real booking — and none of these tests confirm messages.</summary>
[Collection("Sandbox")]
public sealed class SandboxSubmitTests(SandboxFixture fixture, ITestOutputHelper output)
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctBytes_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, bytes);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, bytes): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctBytesCompressed_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, bytes, compress: true);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, bytes, compress=true): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctString_ReturnsCreated()
    {
        var xml = Pain001Writer.WriteToString(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, xml);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, string): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_CctStream_ReturnsCreated()
    {
        var bytes = Pain001Writer.WriteToBytes(BuildCreditTransferInitiation(NewTestMessageId()), Pain001Version.V09);
        using var stream = new MemoryStream(bytes);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, stream);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CCT, stream): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_StructurallyInvalidXml_ReturnsCreated()
    {
        // The sandbox does not validate the submitted body structurally against the order type's
        // schema: a bare `<Document/>` root is accepted like a well-formed pain.001 message.
        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, "<Document/>");

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(structurally invalid XML): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task SubmitOrderAsync_Cdd_ReturnsCreated()
    {
        // The sandbox accepts CDD (SEPA direct debit CORE) submissions like any other order type;
        // it does not enforce mandate or scheme-specific business rules on the mock endpoint.
        var bytes = Pain008Writer.WriteToBytes(BuildDirectDebitInitiation(NewTestMessageId()), Pain008Version.V08);

        var result = await fixture.Client.SubmitOrderAsync(OrderType.CDD, bytes);

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        output.WriteLine($"SubmitOrderAsync(CDD): status={(int)result.StatusCode}, hasLocation={result.Location is not null}, hasBody={result.RawResponse is not null}.");
    }

    // "BANKING-NET-TEST-" (17 chars) + a millisecond-resolution yyyyMMddHHmmssfff timestamp (17
    // chars) + a random alphanumeric character is exactly 35, the ISO 20022 Max35Text limit
    // enforced by PainValidator for these identifiers; the millisecond resolution plus random
    // suffix keep message IDs unique across submissions within the same test run.
    private static string NewTestMessageId() =>
        $"BANKING-NET-TEST-{DateTime.UtcNow:yyyyMMddHHmmssfff}{RandomSuffixChar()}";

    private static char RandomSuffixChar()
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return alphabet[Random.Shared.Next(alphabet.Length)];
    }

    private static CreditTransferInitiation BuildCreditTransferInitiation(string messageId)
    {
        var suffix = messageId[^18..];
        return new CreditTransferInitiation
        {
            MessageId = messageId,
            InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
            PaymentInformations =
            [
                new CreditTransferPaymentInformation
                {
                    PaymentInformationId = "PMTINF-" + suffix,
                    RequestedExecutionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                    DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                    Transactions =
                    [
                        new CreditTransferTransaction
                        {
                            EndToEndId = "E2E-" + suffix,
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
        var suffix = messageId[^18..];
        return new DirectDebitInitiation
        {
            MessageId = messageId,
            InitiatingParty = new PartyIdentification { Name = "Example Creditor Ltd" },
            PaymentInformations =
            [
                new DirectDebitPaymentInformation
                {
                    PaymentInformationId = "PMTINF-" + suffix,
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
                            EndToEndId = "E2E-" + suffix,
                            Amount = new Money(1.00m, "EUR"),
                            Mandate = new MandateInformation { MandateId = "MANDATE-" + suffix, DateOfSignature = new DateOnly(2026, 1, 10) },
                            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
                            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
                        },
                    ],
                },
            ],
        };
    }
}
