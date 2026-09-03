using System.Net;
using Banking.NET.Commerzbank.CorporatePayments;
using Banking.NET.Commerzbank.Exceptions;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>
/// Exercises message confirmation against the live sandbox. Tests that can permanently stop
/// redelivery of a bank-generated message only run when explicitly opted into via
/// <c>COMMERZBANK_SANDBOX_ALLOW_CONFIRM</c> (default: skipped, in CI and locally); a test that
/// cannot affect redelivery of a real message runs under the normal sandbox credential gate.
/// </summary>
[Collection("Sandbox")]
public sealed class SandboxConfirmTests(SandboxFixture fixture, ITestOutputHelper output)
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";
    private const string ConfirmSkipReason = "Set COMMERZBANK_SANDBOX_ALLOW_CONFIRM=1 (in addition to the sandbox credentials) to run tests that confirm sandbox messages.";

    [Fact(SkipUnless = nameof(SandboxCredentials.ConfirmAllowed), SkipType = typeof(SandboxCredentials), Skip = ConfirmSkipReason)]
    public async Task FetchMessagesAsync_HacWithBreakAfterFirst_LeavesMessageUnconfirmed()
    {
        string? firstMessageId = null;
        await foreach (var message in fixture.Client.FetchMessagesAsync(OrderType.HAC, confirm: true))
        {
            firstMessageId = message.MessageId;
            break;
        }
        Assert.SkipWhen(firstMessageId is null, "No HAC message listed in the sandbox.");

        // Per the documented semantics, confirmation happens on the *next* MoveNextAsync call, so
        // breaking after the first message must leave it unconfirmed both client- and server-side.
        var afterBreak = await fixture.Client.ListMessagesAsync(OrderType.HAC);
        var stillListed = afterBreak.Any(m => m.MessageId == firstMessageId);
        output.WriteLine($"HAC message still listed after FetchMessagesAsync + break: {stillListed}.");

        stillListed.ShouldBeTrue();
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task ConfirmMessageAsync_UnknownMessageId_ThrowsNotFound()
    {
        var exception = await Should.ThrowAsync<CommerzbankNotFoundException>(() => fixture.Client.ConfirmMessageAsync("00000000-0000-0000-0000-000000000000"));

        output.WriteLine($"ConfirmMessageAsync with an unknown id: exception={exception.GetType().Name}, status={(int?)exception.StatusCode}, correlationId present={!string.IsNullOrEmpty(exception.CorrelationId)}.");

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.CorrelationId.ShouldNotBeNullOrEmpty();
    }
}
