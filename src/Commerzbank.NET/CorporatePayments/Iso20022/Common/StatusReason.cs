using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A reason attached to a status, e.g. a rejection reason (ISO 20022 <c>StatusReasonInformation</c>).</summary>
public sealed class StatusReason
{
    /// <summary>The coded reason (<c>Rsn/Cd</c>).</summary>
    public string? Code { get; set; }

    /// <summary>The proprietary reason (<c>Rsn/Prtry</c>).</summary>
    public string? Proprietary { get; set; }

    /// <summary>The party that assigned the reason (<c>Orgtr</c>).</summary>
    public PartyIdentification? Originator { get; set; }

    /// <summary>Additional free-form information about the reason (<c>AddtlInf</c>).</summary>
    public List<string> AdditionalInformation { get; set; } = [];

    /// <summary>The XML element this reason was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
