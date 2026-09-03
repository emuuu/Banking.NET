namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Specifies which party bears the charges of a payment transaction (ISO 20022 <c>ChargeBearerType1Code</c>, <c>ChrgBr</c>).</summary>
public enum ChargeBearer
{
    /// <summary>Charges are applied following the rules of the service level (<c>SLEV</c>; for SEPA this means shared between debtor and creditor). The default for SEPA payments.</summary>
    Slev,

    /// <summary>Charges are shared between debtor and creditor (<c>SHAR</c>).</summary>
    Shar,

    /// <summary>All charges are borne by the debtor (<c>DEBT</c>).</summary>
    Debt,

    /// <summary>All charges are borne by the creditor (<c>CRED</c>).</summary>
    Cred,
}
