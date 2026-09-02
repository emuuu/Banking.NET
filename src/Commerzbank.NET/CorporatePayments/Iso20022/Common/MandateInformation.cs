using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Mandate-related information for a direct debit (ISO 20022 <c>MandateRelatedInformation</c>).</summary>
public sealed class MandateInformation
{
    /// <summary>The mandate identifier (<c>MndtId</c>).</summary>
    public string? MandateId { get; set; }

    /// <summary>The date the mandate was signed (<c>DtOfSgntr</c>).</summary>
    public DateOnly? DateOfSignature { get; set; }

    /// <summary>Whether the mandate was amended (<c>AmdmntInd</c>).</summary>
    public bool? AmendmentIndicator { get; set; }

    /// <summary>The original mandate identifier, before the amendment (<c>AmdmntInfDtls/OrgnlMndtId</c>).</summary>
    public string? OriginalMandateId { get; set; }

    /// <summary>The original creditor scheme identifier, before the amendment (<c>AmdmntInfDtls/OrgnlCdtrSchmeId/Id/PrvtId/Othr/Id</c>).</summary>
    public string? OriginalCreditorSchemeId { get; set; }

    /// <summary>The original creditor name, before the amendment (<c>AmdmntInfDtls/OrgnlCdtrSchmeId/Nm</c>).</summary>
    public string? OriginalCreditorName { get; set; }

    /// <summary>The original debtor account, before the amendment (<c>AmdmntInfDtls/OrgnlDbtrAcct</c>).</summary>
    public AccountIdentification? OriginalDebtorAccount { get; set; }

    /// <summary>The original debtor agent, before the amendment (<c>AmdmntInfDtls/OrgnlDbtrAgt</c>).</summary>
    public FinancialInstitution? OriginalDebtorAgent { get; set; }

    /// <summary>The XML element this mandate information was read from, or <see langword="null"/> when not read from XML.</summary>
    public XElement? Source { get; set; }
}
