namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>The pain.008 (customer direct debit initiation) schema version <see cref="Pain008Writer"/> writes.</summary>
public enum Pain008Version
{
    /// <summary><c>urn:iso:std:iso:20022:tech:xsd:pain.008.001.02</c>.</summary>
    V02,

    /// <summary><c>urn:iso:std:iso:20022:tech:xsd:pain.008.001.08</c>.</summary>
    V08,
}
