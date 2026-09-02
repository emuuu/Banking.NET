using System.IO.Compression;
using System.Text;
using Commerzbank.NET.CorporatePayments;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.CorporatePayments;

public class CorporatePaymentsMessageTests
{
    private static byte[] Gzip(string text)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(Encoding.UTF8.GetBytes(text));
        return compressed.ToArray();
    }

    [Fact]
    public void IsGzip_ContentStartsWithGzipMagic_ReturnsTrue()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = Gzip("<Document/>") };
        message.IsGzip.ShouldBeTrue();
    }

    [Fact]
    public void IsGzip_PlainXmlContent_ReturnsFalse()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = "<Document/>"u8.ToArray() };
        message.IsGzip.ShouldBeFalse();
    }

    [Fact]
    public void IsGzip_ContentShorterThanTwoBytes_ReturnsFalse()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = [0x1F] };
        message.IsGzip.ShouldBeFalse();
    }

    [Fact]
    public void OpenContentStream_GzipContent_DecompressesByDefault()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = Gzip("<Document/>") };

        using var stream = message.OpenContentStream();
        using var reader = new StreamReader(stream);
        reader.ReadToEnd().ShouldBe("<Document/>");
    }

    [Fact]
    public void OpenContentStream_GzipContentWithDecompressFalse_ReturnsRawBytes()
    {
        var gzipped = Gzip("<Document/>");
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = gzipped };

        using var stream = message.OpenContentStream(decompress: false);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.ToArray().ShouldBe(gzipped);
    }

    [Fact]
    public void OpenContentStream_PlainContent_ReturnsRawBytes()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = "<Document/>"u8.ToArray() };

        using var stream = message.OpenContentStream();
        using var reader = new StreamReader(stream);
        reader.ReadToEnd().ShouldBe("<Document/>");
    }

    [Fact]
    public void GetContentAsString_GzipContent_ReturnsDecompressedText()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = Gzip("<Document>pain.001</Document>") };
        message.GetContentAsString().ShouldBe("<Document>pain.001</Document>");
    }

    [Fact]
    public void GetContentAsString_Utf8WithBom_StripsBom()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("<Document/>")).ToArray();
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = bytes };

        message.GetContentAsString().ShouldBe("<Document/>");
    }

    [Fact]
    public void GetContentAsString_Utf8WithoutBom_ReturnsText()
    {
        var message = new CorporatePaymentsMessage { MessageId = "msg-1", Content = "<Document/>"u8.ToArray() };
        message.GetContentAsString().ShouldBe("<Document/>");
    }
}
