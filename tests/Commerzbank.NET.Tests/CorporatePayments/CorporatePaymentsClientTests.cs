using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Commerzbank.NET.CorporatePayments;
using Commerzbank.NET.Exceptions;
using Commerzbank.NET.Tests.Helpers;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.CorporatePayments;

public class CorporatePaymentsClientTests
{
    private static readonly Uri SandboxBaseAddress = new("https://api-sandbox.commerzbank.com/");

    private static CorporatePaymentsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = SandboxBaseAddress };
        return new CorporatePaymentsClient(httpClient);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ByteResponse(HttpStatusCode status, byte[] content, string? contentType = "application/xml")
    {
        var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(content) };
        if (contentType is not null)
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return response;
    }

    private static byte[] Gzip(byte[] content)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(content);
        return compressed.ToArray();
    }

    /// <summary>A stream whose read operations throw, used to prove a caller validated arguments before attempting to read it.</summary>
    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("The stream must not be read before order type validation.");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The stream must not be read before order type validation.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The stream must not be read before order type validation.");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new CorporatePaymentsClient(null!));
    }

    [Fact]
    public void Constructor_HttpClientWithoutBaseAddress_ThrowsArgumentException()
    {
        using var httpClient = new HttpClient();
        Should.Throw<ArgumentException>(() => new CorporatePaymentsClient(httpClient));
    }

    [Fact]
    public async Task HeartbeatAsync_RequestsHeartbeatEndpoint()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await client.HeartbeatAsync();

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/heartbeat"));
    }

    [Fact]
    public async Task ListMessagesAsync_WithoutOrderType_RequestsMessagesWithoutQuery()
    {
        var handler = new MockHttpMessageHandler(JsonResponse(HttpStatusCode.OK, "[]"));
        var client = CreateClient(handler);

        await client.ListMessagesAsync();

        handler.LastRequest!.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages"));
    }

    [Fact]
    public async Task ListMessagesAsync_WithOrderType_RequestsMessagesWithOrderTypeQuery()
    {
        var handler = new MockHttpMessageHandler(JsonResponse(HttpStatusCode.OK, "[]"));
        var client = CreateClient(handler);

        await client.ListMessagesAsync(OrderType.C53);

        handler.LastRequest!.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages?OrderType=C53"));
    }

    [Fact]
    public async Task ListMessagesAsync_WithBody_ReturnsMappedMessages()
    {
        const string json = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":2,"Size":1024}]""";
        var handler = new MockHttpMessageHandler(JsonResponse(HttpStatusCode.OK, json));
        var client = CreateClient(handler);

        var result = await client.ListMessagesAsync();

        result.Count.ShouldBe(1);
        result[0].MessageId.ShouldBe("msg-1");
        result[0].OrderType.ShouldBe(OrderType.C53);
        result[0].Fragments.ShouldBe(2);
        result[0].Size.ShouldBe(1024);
    }

    [Fact]
    public async Task ListMessagesAsync_NoContent_ReturnsEmptyList()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        (await client.ListMessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ListMessagesAsync_EmptyBody_ReturnsEmptyList()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        var client = CreateClient(handler);

        (await client.ListMessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task DownloadFragmentAsync_Status200_IsNotPartial()
    {
        var handler = new MockHttpMessageHandler(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()));
        var client = CreateClient(handler);

        var fragment = await client.DownloadFragmentAsync("msg-1", 0);

        fragment.IsPartial.ShouldBeFalse();
        fragment.Index.ShouldBe(0);
        fragment.Content.ShouldBe("content"u8.ToArray());
        fragment.ContentType.ShouldBe("application/xml");
    }

    [Fact]
    public async Task DownloadFragmentAsync_Status206_IsPartial()
    {
        var handler = new MockHttpMessageHandler(ByteResponse(HttpStatusCode.PartialContent, "content"u8.ToArray()));
        var client = CreateClient(handler);

        var fragment = await client.DownloadFragmentAsync("msg-1", 0);

        fragment.IsPartial.ShouldBeTrue();
    }

    [Fact]
    public async Task DownloadFragmentAsync_RequestsExplicitFragmentQuery()
    {
        var handler = new MockHttpMessageHandler(ByteResponse(HttpStatusCode.OK, []));
        var client = CreateClient(handler);

        await client.DownloadFragmentAsync("msg-1", 3);

        handler.LastRequest!.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages/msg-1?Fragment=3"));
    }

    [Fact]
    public async Task DownloadFragmentAsync_EscapesMessageId()
    {
        var handler = new MockHttpMessageHandler(ByteResponse(HttpStatusCode.OK, []));
        var client = CreateClient(handler);

        await client.DownloadFragmentAsync("msg/with space", 0);

        handler.LastRequest!.RequestUri!.AbsoluteUri.ShouldContain(Uri.EscapeDataString("msg/with space"));
    }

    [Fact]
    public async Task DownloadFragmentAsync_Status416_ThrowsCommerzbankApiException()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable));
        var client = CreateClient(handler);

        await Should.ThrowAsync<CommerzbankApiException>(() => client.DownloadFragmentAsync("msg-1", 5));
    }

    [Fact]
    public async Task DownloadFragmentAsync_Status416_ThrowsWithStatusCodeAndCorrelationId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Headers.Add("X-CorrelationID", "corr-416");
        var handler = new MockHttpMessageHandler(response);
        var client = CreateClient(handler);

        var exception = await Should.ThrowAsync<CommerzbankApiException>(() => client.DownloadFragmentAsync("msg-1", 5));

        exception.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        exception.CorrelationId.ShouldBe("corr-416");
    }

    [Fact]
    public async Task DownloadFragmentAsync_UnexpectedSuccessStatus_ThrowsWithStatusCodeCorrelationIdAndRawResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("unexpected body") };
        response.Headers.Add("X-CorrelationID", "corr-202");
        var handler = new MockHttpMessageHandler(response);
        var client = CreateClient(handler);

        var exception = await Should.ThrowAsync<CommerzbankApiException>(() => client.DownloadFragmentAsync("msg-1", 0));

        exception.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        exception.CorrelationId.ShouldBe("corr-202");
        exception.RawResponse.ShouldBe("unexpected body");
    }

    [Fact]
    public async Task DownloadMessageAsync_413ThenIndexZero416_ThrowsWithCorrelationIdAndRawResponse()
    {
        var probeResponse = new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge);
        var fragmentResponse = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable) { Content = new StringContent("fragment 0 unavailable") };
        fragmentResponse.Headers.Add("X-CorrelationID", "corr-413-416");
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(probeResponse)
            .Enqueue(fragmentResponse);
        var client = CreateClient(handler);

        var exception = await Should.ThrowAsync<CommerzbankApiException>(() => client.DownloadMessageAsync("msg-1"));

        exception.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
        exception.CorrelationId.ShouldBe("corr-413-416");
        exception.RawResponse.ShouldBe("fragment 0 unavailable");
        handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DownloadMessageAsync_NoParamReturns200_YieldsSingleFragment()
    {
        var handler = new ScriptedHttpMessageHandler().Enqueue(ByteResponse(HttpStatusCode.OK, "full-content"u8.ToArray()));
        var client = CreateClient(handler);

        var message = await client.DownloadMessageAsync("msg-1");

        message.FragmentCount.ShouldBe(1);
        message.Content.ShouldBe("full-content"u8.ToArray());
        message.ContentType.ShouldBe("application/xml");
        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].RequestUri!.Query.ShouldBe("");
    }

    [Fact]
    public async Task DownloadMessageAsync_NoParam206ThenFragment1Returns200_ConcatenatesTwoFragments()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "part1-"u8.ToArray()))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "part2"u8.ToArray()));
        var client = CreateClient(handler);

        var message = await client.DownloadMessageAsync("msg-1");

        message.FragmentCount.ShouldBe(2);
        message.Content.ShouldBe("part1-part2"u8.ToArray());
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].RequestUri!.Query.ShouldBe("");
        handler.Requests[1].RequestUri!.Query.ShouldBe("?Fragment=1");
    }

    [Fact]
    public async Task DownloadMessageAsync_NoParam413ThenFragmentLoop_StopsAt416()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge))
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "frag0"u8.ToArray()))
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "frag1"u8.ToArray()))
            .Enqueue(new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable));
        var client = CreateClient(handler);

        var message = await client.DownloadMessageAsync("msg-1");

        message.FragmentCount.ShouldBe(2);
        message.Content.ShouldBe("frag0frag1"u8.ToArray());
        handler.Requests.Count.ShouldBe(4);
        handler.Requests[0].RequestUri!.Query.ShouldBe("");
        handler.Requests[1].RequestUri!.Query.ShouldBe("?Fragment=0");
        handler.Requests[2].RequestUri!.Query.ShouldBe("?Fragment=1");
        handler.Requests[3].RequestUri!.Query.ShouldBe("?Fragment=2");
    }

    [Fact]
    public async Task DownloadMessageAsync_WithFragmentCount3_MakesExactlyThreeExplicitRequests()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "a"u8.ToArray()))
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "b"u8.ToArray()))
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "c"u8.ToArray()));
        var client = CreateClient(handler);

        using var destination = new MemoryStream();
        var fragments = await client.DownloadMessageAsync("msg-1", destination, fragmentCount: 3);

        fragments.ShouldBe(3);
        destination.ToArray().ShouldBe("abc"u8.ToArray());
        handler.Requests.Count.ShouldBe(3);
        handler.Requests[0].RequestUri!.Query.ShouldBe("?Fragment=0");
        handler.Requests[1].RequestUri!.Query.ShouldBe("?Fragment=1");
        handler.Requests[2].RequestUri!.Query.ShouldBe("?Fragment=2");
    }

    [Fact]
    public async Task DownloadMessageAsync_KnownFragmentCountAtSafetyCap_AllPartialContent_CompletesWithoutCapException()
    {
        var handler = new ScriptedHttpMessageHandler();
        for (var i = 0; i < 10_000; i++)
            handler.Enqueue(ByteResponse(HttpStatusCode.PartialContent, "x"u8.ToArray()));
        var client = CreateClient(handler);

        using var destination = new MemoryStream();
        var fragments = await client.DownloadMessageAsync("msg-1", destination, fragmentCount: 10_000);

        fragments.ShouldBe(10_000);
        destination.Length.ShouldBe(10_000);
        handler.Requests.Count.ShouldBe(10_000);
    }

    [Fact]
    public async Task DownloadMessageAsync_UnknownFragmentCountExceedsSafetyCap_ThrowsCommerzbankApiException()
    {
        var handler = new ScriptedHttpMessageHandler();
        for (var i = 0; i < 10_001; i++)
            handler.Enqueue(ByteResponse(HttpStatusCode.PartialContent, "x"u8.ToArray()));
        var client = CreateClient(handler);

        await Should.ThrowAsync<CommerzbankApiException>(() => client.DownloadMessageAsync("msg-1"));

        handler.Requests.Count.ShouldBe(10_000);
    }

    [Fact]
    public async Task DownloadMessageAsync_WithMessageInfo_UsesFragmentsAndOrderType()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "x"u8.ToArray()))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "y"u8.ToArray()));
        var client = CreateClient(handler);
        var info = new MessageInfo("msg-1", OrderType.C53, Fragments: 2, Size: 2);

        var message = await client.DownloadMessageAsync(info);

        message.MessageId.ShouldBe("msg-1");
        message.OrderType.ShouldBe(OrderType.C53);
        message.FragmentCount.ShouldBe(2);
        message.Content.ShouldBe("xy"u8.ToArray());
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].RequestUri!.Query.ShouldBe("?Fragment=0");
        handler.Requests[1].RequestUri!.Query.ShouldBe("?Fragment=1");
    }

    [Fact]
    public async Task DownloadMessageAsync_StreamOverload_WritesIdenticalBytes()
    {
        var payload = "hello-world"u8.ToArray();
        var handler = new ScriptedHttpMessageHandler().Enqueue(ByteResponse(HttpStatusCode.OK, payload));
        var client = CreateClient(handler);

        using var destination = new MemoryStream();
        var fragments = await client.DownloadMessageAsync("msg-1", destination);

        fragments.ShouldBe(1);
        destination.ToArray().ShouldBe(payload);
    }

    [Fact]
    public async Task DownloadMessageAsync_ContentTypeIsTakenFromFirstResponse()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(ByteResponse(HttpStatusCode.PartialContent, "a"u8.ToArray(), "application/xml"))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "b"u8.ToArray(), "application/octet-stream"));
        var client = CreateClient(handler);

        var message = await client.DownloadMessageAsync("msg-1");

        message.ContentType.ShouldBe("application/xml");
    }

    [Fact]
    public async Task DownloadMessageAsync_Status404_ThrowsCommerzbankNotFoundException()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        await Should.ThrowAsync<CommerzbankNotFoundException>(() => client.DownloadMessageAsync("missing"));
    }

    [Fact]
    public async Task DownloadMessageAsync_Status410_ThrowsCommerzbankGoneException()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Gone));
        var client = CreateClient(handler);

        await Should.ThrowAsync<CommerzbankGoneException>(() => client.DownloadMessageAsync("gone"));
    }

    [Fact]
    public async Task ConfirmMessageAsync_Complete_SendsPutWithCompleteBody()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await client.ConfirmMessageAsync("msg-1");

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        handler.LastRequest.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages/msg-1"));
        handler.LastRequestContent.ShouldBe("""{"received":"complete"}""");
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task ConfirmMessageAsync_Partial_SendsPartialBody()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await client.ConfirmMessageAsync("msg-1", ReceivedStatus.Partial);

        handler.LastRequestContent.ShouldBe("""{"received":"partial"}""");
    }

    [Fact]
    public async Task SubmitOrderAsync_Uncompressed_SetsOrderTypeHeaderAndXmlContentType()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(handler);

        await client.SubmitOrderAsync(OrderType.CCT, "<Document/>");

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri.ShouldBe(new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages"));
        handler.LastRequest.Headers.GetValues("OrderType").ShouldBe(["CCT"]);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.ShouldBe("application/xml");
        handler.LastRequestContent.ShouldBe("<Document/>");
    }

    [Fact]
    public async Task SubmitOrderAsync_Compressed_SendsGzippedContentWithGzipContentType()
    {
        byte[]? sentBytes = null;
        string? sentMediaType = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            sentBytes = request.Content!.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            sentMediaType = request.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var client = CreateClient(handler);
        var original = "<Document>pain.001</Document>"u8.ToArray();

        await client.SubmitOrderAsync(OrderType.CCT, original, compress: true);

        sentMediaType.ShouldBe("application/gzip");
        sentBytes.ShouldNotBeNull();
        sentBytes![0].ShouldBe((byte)0x1F);
        sentBytes[1].ShouldBe((byte)0x8B);

        using var decompressed = new GZipStream(new MemoryStream(sentBytes), CompressionMode.Decompress);
        using var memory = new MemoryStream();
        await decompressed.CopyToAsync(memory);
        memory.ToArray().ShouldBe(original);
    }

    [Fact]
    public async Task SubmitOrderAsync_ByteArrayAlreadyGzipped_IsSentWithoutRecompressing()
    {
        byte[]? sentBytes = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            sentBytes = request.Content!.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var client = CreateClient(handler);
        var alreadyGzipped = Gzip("<Document/>"u8.ToArray());

        await client.SubmitOrderAsync(OrderType.CCT, alreadyGzipped, compress: true);

        sentBytes.ShouldBe(alreadyGzipped);
    }

    [Fact]
    public async Task SubmitOrderAsync_ByteArrayAlreadyGzipped_CompressFalse_SetsGzipContentTypeWithoutRecompressing()
    {
        byte[]? sentBytes = null;
        string? sentMediaType = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            sentBytes = request.Content!.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            sentMediaType = request.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var client = CreateClient(handler);
        var alreadyGzipped = Gzip("<Document/>"u8.ToArray());

        await client.SubmitOrderAsync(OrderType.CCT, alreadyGzipped, compress: false);

        sentMediaType.ShouldBe("application/gzip");
        sentBytes.ShouldBe(alreadyGzipped);
    }

    [Fact]
    public async Task SubmitOrderAsync_StreamOverload_SendsStreamContent()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(handler);

        using var stream = new MemoryStream("<Document/>"u8.ToArray());
        await client.SubmitOrderAsync(OrderType.CCT, stream);

        handler.LastRequestContent.ShouldBe("<Document/>");
    }

    [Fact]
    public async Task SubmitOrderAsync_StringOverload_EncodesAsUtf8WithoutBom()
    {
        byte[]? sentBytes = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            sentBytes = request.Content!.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var client = CreateClient(handler);

        await client.SubmitOrderAsync(OrderType.CCT, "<Document/>");

        sentBytes.ShouldBe("<Document/>"u8.ToArray());
    }

    [Fact]
    public async Task SubmitOrderAsync_DownloadOrderType_ThrowsArgumentException()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(handler);

        await Should.ThrowAsync<ArgumentException>(() => client.SubmitOrderAsync(OrderType.C53, "<Document/>"));
    }

    [Fact]
    public async Task SubmitOrderAsync_StreamOverload_DownloadOrderType_ThrowsBeforeReadingStream()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created));
        var client = CreateClient(handler);
        using var stream = new UnreadableStream();

        await Should.ThrowAsync<ArgumentException>(() => client.SubmitOrderAsync(OrderType.C53, stream));
    }

    [Fact]
    public async Task SubmitOrderAsync_Created_ReturnsStatusCodeAndLocation()
    {
        var location = new Uri(SandboxBaseAddress, "corporate-payments-api/1/v1/bulk-payments/messages/msg-99");
        var response = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("submitted") };
        response.Headers.Location = location;
        var handler = new MockHttpMessageHandler(response);
        var client = CreateClient(handler);

        var result = await client.SubmitOrderAsync(OrderType.CCT, "<Document/>");

        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.Location.ShouldBe(location);
        result.RawResponse.ShouldBe("submitted");
    }

    [Fact]
    public async Task FetchMessagesAsync_ConfirmsAfterYield_InListDownloadYieldConfirmOrder()
    {
        const string listJson = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":1,"Size":10}]""";
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(JsonResponse(HttpStatusCode.OK, listJson))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()))
            .Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await using var enumerator = client.FetchMessagesAsync().GetAsyncEnumerator();

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[1].Method.ShouldBe(HttpMethod.Get);
        enumerator.Current.MessageId.ShouldBe("msg-1");

        (await enumerator.MoveNextAsync()).ShouldBeFalse();
        handler.Requests.Count.ShouldBe(3);
        handler.Requests[2].Method.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    public async Task FetchMessagesAsync_ConfirmFalse_DoesNotSendPut()
    {
        const string listJson = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":1,"Size":10}]""";
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(JsonResponse(HttpStatusCode.OK, listJson))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()));
        var client = CreateClient(handler);

        var messages = new List<CorporatePaymentsMessage>();
        await foreach (var message in client.FetchMessagesAsync(confirm: false))
            messages.Add(message);

        messages.Count.ShouldBe(1);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests.ShouldNotContain(request => request.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task FetchMessagesAsync_ConfirmFails_ThrowsMessageConfirmationExceptionOnNextMoveNext()
    {
        const string listJson = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":1,"Size":10}]""";
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(JsonResponse(HttpStatusCode.OK, listJson))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()))
            .Enqueue(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        await using var enumerator = client.FetchMessagesAsync().GetAsyncEnumerator();
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        var downloaded = enumerator.Current;

        var exception = await Should.ThrowAsync<MessageConfirmationException>(() => enumerator.MoveNextAsync().AsTask());
        exception.MessageId.ShouldBe("msg-1");
        exception.DownloadedMessage.ShouldBeSameAs(downloaded);
    }

    [Fact]
    public async Task FetchMessagesAsync_ConfirmFailsWithCorrelationId_ThrowsMessageConfirmationExceptionWithStatusAndCorrelationId()
    {
        const string listJson = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":1,"Size":10}]""";
        var confirmFailure = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        confirmFailure.Headers.Add("X-CorrelationID", "corr-confirm-500");
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(JsonResponse(HttpStatusCode.OK, listJson))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()))
            .Enqueue(confirmFailure);
        var client = CreateClient(handler);

        await using var enumerator = client.FetchMessagesAsync().GetAsyncEnumerator();
        (await enumerator.MoveNextAsync()).ShouldBeTrue();

        var exception = await Should.ThrowAsync<MessageConfirmationException>(() => enumerator.MoveNextAsync().AsTask());

        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.CorrelationId.ShouldBe("corr-confirm-500");
        exception.InnerException.ShouldNotBeNull();
    }

    [Fact]
    public async Task FetchMessagesAsync_BreakAfterFirstElement_DoesNotConfirm()
    {
        const string listJson = """[{"MessageId":"msg-1","OrderType":"C53","Fragments":1,"Size":10},{"MessageId":"msg-2","OrderType":"C53","Fragments":1,"Size":10}]""";
        var handler = new ScriptedHttpMessageHandler()
            .Enqueue(JsonResponse(HttpStatusCode.OK, listJson))
            .Enqueue(ByteResponse(HttpStatusCode.OK, "content"u8.ToArray()));
        var client = CreateClient(handler);

        await foreach (var message in client.FetchMessagesAsync())
        {
            message.MessageId.ShouldBe("msg-1");
            break;
        }

        handler.Requests.Count.ShouldBe(2);
        handler.Requests.ShouldNotContain(request => request.Method == HttpMethod.Put);
    }
}
