namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Options controlling how <see cref="Pain001Writer"/> and <see cref="Pain008Writer"/> render a document.</summary>
public sealed class Pain00xWriterOptions
{
    /// <summary>
    /// Whether to validate that names, unstructured remittance information, end-to-end identifiers and other
    /// identifiers use only the SEPA character set (<see cref="SepaCharacterSet.IsValid(string)"/>). Disabled by
    /// default; the writer never sanitizes text on its own — the caller decides whether and how to do so, e.g. via
    /// <see cref="SepaCharacterSet.Sanitize(string)"/>, before constructing the model.
    /// </summary>
    public bool ValidateCharacterSet { get; set; }

    /// <summary>Whether to omit the XML declaration from the written document. Disabled by default.</summary>
    public bool OmitXmlDeclaration { get; set; }
}
