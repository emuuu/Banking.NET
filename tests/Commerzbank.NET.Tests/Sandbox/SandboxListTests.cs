using Commerzbank.NET.CorporatePayments;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Sandbox;

/// <summary>Verifies <see cref="ICorporatePaymentsClient.ListMessagesAsync"/> against the live sandbox.</summary>
public sealed class SandboxListTests(SandboxFixture fixture, ITestOutputHelper output) : IClassFixture<SandboxFixture>
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task ListMessagesAsync_Sandbox_ReturnsWellFormedEntries()
    {
        var messages = await fixture.Client.ListMessagesAsync();

        messages.ShouldNotBeEmpty();
        foreach (var message in messages)
        {
            message.MessageId.ShouldNotBeNullOrWhiteSpace();
            message.OrderType.Code.Length.ShouldBe(3);
            message.Fragments.ShouldBeGreaterThanOrEqualTo(1);
            message.Size.ShouldBeGreaterThan(0);
        }

        var distribution = messages.GroupBy(m => m.OrderType.Code).OrderBy(g => g.Key).Select(g => $"{g.Key}={g.Count()}");
        output.WriteLine($"Listed {messages.Count} message(s): {string.Join(", ", distribution)}");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task ListMessagesAsync_FilteredByOrderType_Sandbox_ReturnsSubsetOfFullList()
    {
        var all = await fixture.Client.ListMessagesAsync();
        var filtered = await fixture.Client.ListMessagesAsync(OrderType.C53);

        // The gateway's OrderType filtering behavior is not documented; this only asserts the
        // filtered result is a subset of the unfiltered list. Whether it actually narrows down to
        // C53 is logged below and reconciled against the assumption in the report.
        var allIds = all.Select(m => m.MessageId).ToHashSet();
        filtered.ShouldAllBe(m => allIds.Contains(m.MessageId));

        var exclusivelyC53 = filtered.Count > 0 && filtered.All(m => m.OrderType == OrderType.C53);
        var returnedTypes = string.Join(", ", filtered.Select(m => m.OrderType.Code).Distinct().OrderBy(code => code));
        output.WriteLine($"Filtered by C53: {filtered.Count} of {all.Count} total; exclusively C53 = {exclusivelyC53}; order types returned: {returnedTypes}");
    }
}
