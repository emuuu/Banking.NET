using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A group of billing statements for one relationship between a sender and a receiver (<c>BllgStmtGrp</c>).</summary>
public sealed class BillingStatementGroup
{
    /// <summary>The group identifier (<c>GrpId</c>).</summary>
    public string? GroupId { get; init; }

    /// <summary>The party sending the statement (<c>Sndr</c>).</summary>
    public PartyIdentification? Sender { get; init; }

    /// <summary>The party receiving the statement (<c>Rcvr</c>).</summary>
    public PartyIdentification? Receiver { get; init; }

    /// <summary>The billing statements in this group (<c>BllgStmt</c>).</summary>
    public List<BillingStatement> Statements { get; init; } = [];

    /// <summary>The <c>BllgStmtGrp</c> element this group was read from.</summary>
    public required XElement Source { get; init; }
}
