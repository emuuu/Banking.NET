using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A bank-to-customer camt.052 (report), camt.053 (statement) or camt.054 (notification) message.</summary>
public sealed class BankToCustomerMessage
{
    /// <summary>The message's identified type, schema identifier and namespace.</summary>
    public required Iso20022MessageIdentifier Identifier { get; init; }

    /// <summary>The message's group header (<c>GrpHdr</c>).</summary>
    public required GroupHeader GroupHeader { get; init; }

    /// <summary>The reports, statements or notifications carried by the message (<c>Rpt</c>/<c>Stmt</c>/<c>Ntfctn</c>).</summary>
    public List<AccountStatement> Statements { get; init; } = [];

    /// <summary>The <c>BkToCstmrAcctRpt</c>/<c>BkToCstmrStmt</c>/<c>BkToCstmrDbtCdtNtfctn</c> element this message was read from.</summary>
    public required XElement Source { get; init; }
}
