using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The group header of a camt.052/053/054 message (<c>GrpHdr</c>).</summary>
public sealed class GroupHeader
{
    /// <summary>The message identifier (<c>MsgId</c>).</summary>
    public string? MessageId { get; init; }

    /// <summary>The message creation date and time (<c>CreDtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? CreationDateTime { get; init; }

    /// <summary>The name of the intended message recipient (<c>MsgRcpt/Nm</c>).</summary>
    public string? MessageRecipientName { get; init; }

    /// <summary>The page number, for paginated messages (<c>MsgPgntn/PgNb</c>).</summary>
    public int? PageNumber { get; init; }

    /// <summary>Whether this is the last page (<c>MsgPgntn/LastPgInd</c>).</summary>
    public bool? LastPageIndicator { get; init; }

    /// <summary>Additional free-form information (<c>AddtlInf</c>).</summary>
    public string? AdditionalInformation { get; init; }

    /// <summary>The <c>GrpHdr</c> element this group header was read from.</summary>
    public required XElement Source { get; init; }
}
