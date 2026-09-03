using Banking.NET.Commerzbank.CorporatePayments;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>Verifies <see cref="ICorporatePaymentsClient.ListMessagesAsync"/> against the live sandbox.</summary>
[Collection("Sandbox")]
public sealed class SandboxListTests(SandboxFixture fixture, ITestOutputHelper output)
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task ListMessagesAsync_Unfiltered_ReturnsWellFormedEntries()
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
    public async Task ListMessagesAsync_FilteredByOrderType_ReturnsOnlyThatOrderType()
    {
        var all = await fixture.Client.ListMessagesAsync();
        var filtered = await fixture.Client.ListMessagesAsync(OrderType.C53);

        var allIds = all.Select(m => m.MessageId).ToHashSet();
        filtered.ShouldAllBe(m => allIds.Contains(m.MessageId));
        filtered.ShouldAllBe(m => m.OrderType == OrderType.C53);

        output.WriteLine($"Filtered by C53: {filtered.Count} of {all.Count} total.");
    }
}
