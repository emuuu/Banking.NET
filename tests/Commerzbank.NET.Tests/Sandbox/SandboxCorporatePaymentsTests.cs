using Commerzbank.NET.Auth;
using Commerzbank.NET.CorporatePayments;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Sandbox;

/// <summary>
/// Integration tests against the live Commerzbank sandbox. They skip unless
/// <see cref="SandboxCredentials.Available"/>, and they only submit orders and log observed
/// behavior; they never confirm messages automatically, so downloaded messages remain
/// available for later verification.
/// </summary>
public sealed class SandboxCorporatePaymentsTests(SandboxFixture fixture, ITestOutputHelper output) : IClassFixture<SandboxFixture>
{
    private const string SamplePain001 = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:pain.001.001.09">
          <CstmrCdtTrfInitn>
            <GrpHdr>
              <MsgId>SANDBOX-TEST-0001</MsgId>
              <CreDtTm>2026-09-02T10:00:00</CreDtTm>
              <NbOfTxs>1</NbOfTxs>
              <CtrlSum>100.00</CtrlSum>
              <InitgPty>
                <Nm>Example Debtor GmbH</Nm>
              </InitgPty>
            </GrpHdr>
            <PmtInf>
              <PmtInfId>SANDBOX-TEST-0001-1</PmtInfId>
              <PmtMtd>TRF</PmtMtd>
              <NbOfTxs>1</NbOfTxs>
              <CtrlSum>100.00</CtrlSum>
              <ReqdExctnDt>
                <Dt>2026-09-03</Dt>
              </ReqdExctnDt>
              <Dbtr>
                <Nm>Example Debtor GmbH</Nm>
              </Dbtr>
              <DbtrAcct>
                <Id>
                  <IBAN>DE89370400440532013000</IBAN>
                </Id>
              </DbtrAcct>
              <DbtrAgt>
                <FinInstnId>
                  <BICFI>COBADEFFXXX</BICFI>
                </FinInstnId>
              </DbtrAgt>
              <CdtTrfTxInf>
                <PmtId>
                  <EndToEndId>SANDBOX-TEST-0001-1-1</EndToEndId>
                </PmtId>
                <Amt>
                  <InstdAmt Ccy="EUR">100.00</InstdAmt>
                </Amt>
                <CdtrAgt>
                  <FinInstnId>
                    <BICFI>MARKDEF1100</BICFI>
                  </FinInstnId>
                </CdtrAgt>
                <Cdtr>
                  <Nm>Example Creditor Ltd</Nm>
                </Cdtr>
                <CdtrAcct>
                  <Id>
                    <IBAN>DE02120300000000202051</IBAN>
                  </Id>
                </CdtrAcct>
              </CdtTrfTxInf>
            </PmtInf>
          </CstmrCdtTrfInitn>
        </Document>
        """;

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.")]
    public async Task HeartbeatAsync_Sandbox_Succeeds()
    {
        await fixture.Client.HeartbeatAsync();
        output.WriteLine("Heartbeat succeeded.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.")]
    public async Task GetAccessTokenAsync_Sandbox_ReturnsTokenWithExpiry()
    {
        var tokenProvider = (ClientCredentialsTokenProvider)fixture.Services.GetRequiredService<IAccessTokenProvider>();
        await tokenProvider.GetAccessTokenAsync();

        var token = tokenProvider.CurrentToken;
        token.ShouldNotBeNull();
        output.WriteLine($"Access token obtained, expires at {token!.ExpiresAt:O} (in {token.ExpiresAt - DateTimeOffset.UtcNow}).");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.")]
    public async Task ListMessagesAsync_Sandbox_LogsObservedMessages()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        output.WriteLine($"Listed {messages.Count} message(s).");
        foreach (var message in messages)
            output.WriteLine($"  {message.MessageId}: OrderType={message.OrderType}, Fragments={message.Fragments}, Size={message.Size}");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.")]
    public async Task DownloadFirstListedMessage_Sandbox_LogsObservedBehavior()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        if (messages.Count == 0)
        {
            output.WriteLine("No messages available to download.");
            return;
        }

        var first = messages[0];

        var fragment = await fixture.Client.DownloadFragmentAsync(first.MessageId, 0);
        output.WriteLine($"DownloadFragmentAsync({first.MessageId}, 0): IsPartial={fragment.IsPartial}, ContentType={fragment.ContentType}, Length={fragment.Content.Length}");

        var message = await fixture.Client.DownloadMessageAsync(first);
        var text = message.GetContentAsString();
        var preview = text.Length > 200 ? text[..200] : text;
        output.WriteLine($"DownloadMessageAsync({first.MessageId}): FragmentCount={message.FragmentCount}, Preview={preview}");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.")]
    public async Task SubmitOrderAsync_Sandbox_LogsObservedResult()
    {
        var result = await fixture.Client.SubmitOrderAsync(OrderType.CCT, SamplePain001);
        output.WriteLine($"SubmitOrderAsync(CCT): StatusCode={(int)result.StatusCode}, Location={result.Location}, Body={result.RawResponse}");
    }

    [Fact(Skip = "Manually enable to confirm a specific sandbox message; confirmation is not run automatically so downloaded messages remain available for later verification.")]
    public async Task ConfirmMessage_Sandbox_ManuallyEnabled()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        if (messages.Count == 0)
        {
            output.WriteLine("No messages available to confirm.");
            return;
        }

        await fixture.Client.ConfirmMessageAsync(messages[0].MessageId);
        output.WriteLine($"Confirmed {messages[0].MessageId}.");
    }
}
