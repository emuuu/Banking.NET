namespace Commerzbank.NET.CorporatePayments;

/// <summary>A single fragment of a message returned by <see cref="ICorporatePaymentsClient.DownloadFragmentAsync"/>.</summary>
/// <param name="Index">The zero-based fragment index that was requested.</param>
/// <param name="Content">The raw fragment bytes.</param>
/// <param name="ContentType">The `Content-Type` of the response, if any.</param>
/// <param name="IsPartial">Whether the gateway returned HTTP 206 Partial Content (more fragments follow) rather than 200 OK.</param>
public sealed record MessageFragment(int Index, byte[] Content, string? ContentType, bool IsPartial);
