using System.IO.Compression;
using System.Text;

namespace Banking.NET.Commerzbank.CorporatePayments;

/// <summary>A downloaded Corporate Payments message with all its fragments concatenated.</summary>
public sealed class CorporatePaymentsMessage
{
    /// <summary>The identifier of the downloaded message.</summary>
    public required string MessageId { get; init; }

    /// <summary>The order type of the message, if known (i.e. the message was loaded via a <see cref="MessageInfo"/>).</summary>
    public OrderType? OrderType { get; init; }

    /// <summary>The concatenated raw bytes of all downloaded fragments.</summary>
    public required byte[] Content { get; init; }

    /// <summary>The `Content-Type` of the first fragment response, if any.</summary>
    public string? ContentType { get; init; }

    /// <summary>The number of fragments that were downloaded.</summary>
    public int FragmentCount { get; init; }

    /// <summary>Whether <see cref="Content"/> starts with the gzip magic bytes 0x1F 0x8B.</summary>
    public bool IsGzip => Content.Length >= 2 && Content[0] == 0x1F && Content[1] == 0x8B;

    /// <summary>Opens a read-only stream over <see cref="Content"/>.</summary>
    /// <param name="decompress">Whether to transparently gunzip the content when <see cref="IsGzip"/> is true. Defaults to true.</param>
    /// <returns>A <see cref="MemoryStream"/> over the raw content, or a <see cref="GZipStream"/> decompressing it.</returns>
    public Stream OpenContentStream(bool decompress = true)
    {
        var stream = new MemoryStream(Content, writable: false);
        return IsGzip && decompress ? new GZipStream(stream, CompressionMode.Decompress) : stream;
    }

    /// <summary>Reads <see cref="Content"/> as text, decompressing it first if <see cref="IsGzip"/>, using the byte order mark to detect the encoding and falling back to UTF-8.</summary>
    /// <returns>The decoded message text.</returns>
    public string GetContentAsString()
    {
        using var stream = OpenContentStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
