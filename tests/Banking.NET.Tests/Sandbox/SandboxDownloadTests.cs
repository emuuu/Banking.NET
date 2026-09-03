using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Banking.NET.Commerzbank.CorporatePayments;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022;
using Banking.NET.Commerzbank.Exceptions;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>Verifies message download, fragmentation and reader compatibility against the live sandbox.</summary>
[Collection("Sandbox")]
public sealed class SandboxDownloadTests(SandboxFixture fixture, ITestOutputHelper output)
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_SingleFragmentAccountMessage_MatchesListedSizeAndParses()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        var info = messages
            .Where(m => (m.OrderType == OrderType.C52 || m.OrderType == OrderType.C53) && m.Fragments == 1)
            .OrderBy(m => m.Size)
            .FirstOrDefault();
        Assert.SkipWhen(info is null, "No single-fragment C52/C53 message listed in the sandbox.");

        var message = await fixture.Client.DownloadMessageAsync(info!);

        ((long)message.Content.Length).ShouldBe(info!.Size);
        message.ContentType.ShouldNotBeNull();
        message.ContentType.ShouldStartWith("application/xml");
        message.FragmentCount.ShouldBe(1);
        message.IsGzip.ShouldBeFalse();

        var identifier = Iso20022Document.Identify(Iso20022Document.Load(message.GetContentAsString()));
        (identifier.Type == Iso20022MessageType.Camt052 || identifier.Type == Iso20022MessageType.Camt053).ShouldBeTrue();

        var bankMessage = CamtReader.Read(message);
        bankMessage.Identifier.Type.ShouldBe(identifier.Type);

        var fragment = await fixture.Client.DownloadFragmentAsync(info.MessageId, 0);
        fragment.IsPartial.ShouldBeFalse();
        fragment.Content.ShouldBe(message.Content);

        output.WriteLine($"Single-fragment {info.OrderType.Code}: size={info.Size}, schema={identifier.Identifier}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_MultiFragmentCamtMessage_ConcatenatesFragmentsConsistently()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;

        var messages = await WithSandboxRetryAsync(() => fixture.Client.ListMessagesAsync(cancellationToken: cancellationToken));
        var info = messages
            .Where(m => m.Fragments > 1 && (m.OrderType == OrderType.C52 || m.OrderType == OrderType.C53 || m.OrderType == OrderType.C54))
            .OrderBy(m => m.Size)
            .FirstOrDefault();
        Assert.SkipWhen(info is null, "No multi-fragment camt.052/053/054 message listed in the sandbox.");

        var stopwatch = Stopwatch.StartNew();

        var partialFlags = new List<bool>();
        using var buffer = new MemoryStream();
        for (var i = 0; i < info!.Fragments; i++)
        {
            var fragment = await WithSandboxRetryAsync(() => fixture.Client.DownloadFragmentAsync(info.MessageId, i, cancellationToken));
            partialFlags.Add(fragment.IsPartial);
            buffer.Write(fragment.Content);
        }
        var concatenated = buffer.ToArray();
        output.WriteLine($"Multi-fragment message {info.OrderType.Code}: {info.Fragments} fragment(s), {info.Size} byte(s) total.");
        output.WriteLine($"Fragment IsPartial sequence for {info.Fragments} fragment(s) of {info.OrderType.Code}: {string.Join(",", partialFlags)}");

        // Deviation from the assumption that only the last known fragment is non-partial: the
        // sandbox answers every known fragment index with 206 (partial), including the last one,
        // and only signals the end via 416 on the index beyond it (asserted below).
        partialFlags.ShouldAllBe(isPartial => isPartial);

        var beyondLast = await WithSandboxRetryAsync(async () =>
        {
            var exception = await Should.ThrowAsync<CommerzbankApiException>(() =>
                fixture.Client.DownloadFragmentAsync(info.MessageId, info.Fragments, cancellationToken));
            return exception.StatusCode is >= HttpStatusCode.InternalServerError ? throw exception : exception;
        });
        beyondLast.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        output.WriteLine($"Fragment beyond last known index: status={(int?)beyondLast.StatusCode}.");

        var downloaded = await WithSandboxRetryAsync(() => fixture.Client.DownloadMessageAsync(info, cancellationToken));
        downloaded.FragmentCount.ShouldBe(info.Fragments);
        ((long)downloaded.Content.Length).ShouldBe(info.Size);
        downloaded.Content.ShouldBe(concatenated);

        var expectedHash = Convert.ToHexString(SHA256.HashData(downloaded.Content));

        var tempFile = Path.GetTempFileName();
        try
        {
            var fragmentsRead = await WithSandboxRetryAsync(async () =>
            {
                await using var destination = File.Create(tempFile);
                return await fixture.Client.DownloadMessageAsync(info.MessageId, destination, info.Fragments, cancellationToken);
            });

            fragmentsRead.ShouldBe(info.Fragments);
            var streamedHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(tempFile, cancellationToken)));
            streamedHash.ShouldBe(expectedHash);
        }
        finally
        {
            File.Delete(tempFile);
        }

        var withoutKnownFragmentCount = await WithSandboxRetryAsync(() => fixture.Client.DownloadMessageAsync(info.MessageId, cancellationToken));
        var withoutKnownCountHash = Convert.ToHexString(SHA256.HashData(withoutKnownFragmentCount.Content));
        withoutKnownCountHash.ShouldBe(expectedHash);
        output.WriteLine($"DownloadMessageAsync without a known fragment count observed {withoutKnownFragmentCount.FragmentCount} fragment(s) (reveals which of the 413/206 paths the sandbox takes).");

        CamtReader.Read(withoutKnownFragmentCount);

        stopwatch.Stop();
        output.WriteLine($"Multi-fragment download test runtime: {stopwatch.Elapsed}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_C54_ParsesAsNotification()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        var info = messages.FirstOrDefault(m => m.OrderType == OrderType.C54);
        Assert.SkipWhen(info is null, "No C54 message listed in the sandbox.");

        var message = await fixture.Client.DownloadMessageAsync(info!);
        var bankMessage = CamtReader.Read(message);

        bankMessage.Statements.ShouldNotBeEmpty();
        bankMessage.Statements[0].Kind.ShouldBe(StatementKind.Notification);
        output.WriteLine($"C54 parsed with {bankMessage.Statements.Count} statement(s).");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_C86_ParsesWithAtLeastOneStatement()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        var info = messages.FirstOrDefault(m => m.OrderType == OrderType.C86);
        Assert.SkipWhen(info is null, "No C86 message listed in the sandbox.");

        var message = await fixture.Client.DownloadMessageAsync(info!);
        var identifier = Iso20022Document.Identify(Iso20022Document.Load(message.GetContentAsString()));
        (identifier.Identifier.EndsWith("camt.086.001.01", StringComparison.Ordinal)
            || identifier.Identifier.EndsWith("camt.086.001.02", StringComparison.Ordinal)).ShouldBeTrue();

        var billing = Camt086Reader.Read(message);

        billing.Groups.ShouldNotBeEmpty();
        var statements = billing.Groups.SelectMany(g => g.Statements).ToList();
        statements.ShouldNotBeEmpty();
        statements[0].StatementId.ShouldNotBeNullOrWhiteSpace();
        statements[0].Account.ShouldNotBeNull();
        statements.Any(s => s.Services.Count > 0 || s.Balances.Count > 0).ShouldBeTrue();

        output.WriteLine($"C86: schema={identifier.Identifier}, groups={billing.Groups.Count}, statements={statements.Count}, services={statements.Sum(s => s.Services.Count)}, balances={statements.Sum(s => s.Balances.Count)}.");
    }

    [Theory(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    [InlineData("XIP")]
    [InlineData("CRZ")]
    [InlineData("CDZ")]
    [InlineData("CIZ")]
    [InlineData("CUZ")]
    [InlineData("AXS")]
    public async Task DownloadMessageAsync_Pain002OrderType_ParsesSuccessfully(string orderTypeCode)
    {
        var orderType = OrderType.Parse(orderTypeCode);
        var messages = await fixture.Client.ListMessagesAsync();
        var info = messages.FirstOrDefault(m => m.OrderType == orderType);
        Assert.SkipWhen(info is null, $"No {orderTypeCode} message listed in the sandbox.");

        var message = await fixture.Client.DownloadMessageAsync(info!);
        var identifier = Iso20022Document.Identify(Iso20022Document.Load(message.GetContentAsString()));
        identifier.Type.ShouldBe(Iso20022MessageType.Pain002);

        var status = Pain002Reader.Read(message);
        status.OriginalGroup.OriginalMessageId.ShouldNotBeNullOrWhiteSpace();
        status.OriginalGroup.OriginalMessageNameId.ShouldNotBeNullOrWhiteSpace();
        // GrpSts is optional in the ISO 20022 schema; the sandbox's mock messages populate it for
        // some order types (e.g. XIP, CRZ, AXS) but not others, which state status only at the
        // payment information or transaction level instead — a legitimate absence, not a reader defect.
        (status.OriginalGroup.GroupStatus is not null || status.PaymentInformations.Count > 0).ShouldBeTrue();

        output.WriteLine($"{orderTypeCode}: schema={identifier.Identifier}, groupStatus={status.OriginalGroup.GroupStatus ?? "(not set at group level)"}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_Hac_LogsIdentifiedMessageType()
    {
        var messages = await fixture.Client.ListMessagesAsync();
        var info = messages.FirstOrDefault(m => m.OrderType == OrderType.HAC);
        Assert.SkipWhen(info is null, "No HAC message listed in the sandbox.");

        var message = await fixture.Client.DownloadMessageAsync(info!);
        var document = Iso20022Document.Load(message.GetContentAsString());
        var identifier = Iso20022Document.Identify(document);
        output.WriteLine($"HAC: type={identifier.Type}, identifier={identifier.Identifier}, namespace={identifier.Namespace}.");

        if (identifier.Type == Iso20022MessageType.Pain002)
        {
            var status = Pain002Reader.Read(message);
            status.OriginalGroup.OriginalMessageId.ShouldNotBeNullOrWhiteSpace();
            status.OriginalGroup.OriginalMessageNameId.ShouldNotBeNullOrWhiteSpace();
        }
        else
        {
            document.Root.ShouldNotBeNull();
            output.WriteLine($"HAC root element: {document.Root!.Name.LocalName}.");
        }
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task DownloadMessageAsync_UnknownMessageId_ThrowsWithObservedStatus()
    {
        var exception = await Should.ThrowAsync<CommerzbankException>(() => fixture.Client.DownloadMessageAsync("00000000-0000-0000-0000-000000000000"));

        output.WriteLine($"Unknown message id: exception={exception.GetType().Name}, status={(int?)exception.StatusCode}, correlationId present={!string.IsNullOrEmpty(exception.CorrelationId)}.");

        exception.ShouldBeOfType<CommerzbankNotFoundException>();
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    // The sandbox was observed to intermittently answer repeated downloads of the same large,
    // multi-fragment message with a transient 500 Internal Server Error; this retries such
    // server-side errors a few times so the test reflects the download logic rather than sandbox
    // load spikes unrelated to it.
    private static async Task<T> WithSandboxRetryAsync<T>(Func<Task<T>> action)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (CommerzbankApiException ex) when (attempt < maxAttempts && ex.StatusCode is >= HttpStatusCode.InternalServerError)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
