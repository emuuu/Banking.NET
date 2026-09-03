namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>The ISO 20022 message type identified from a document's root namespace.</summary>
public enum Iso20022MessageType
{
    /// <summary>The message type could not be identified.</summary>
    Unknown,

    /// <summary>Bank-to-customer account report (<c>camt.052</c>).</summary>
    Camt052,

    /// <summary>Bank-to-customer statement (<c>camt.053</c>).</summary>
    Camt053,

    /// <summary>Bank-to-customer debit/credit notification (<c>camt.054</c>).</summary>
    Camt054,

    /// <summary>Bank services billing statement (<c>camt.086</c>).</summary>
    Camt086,

    /// <summary>Customer credit transfer initiation (<c>pain.001</c>).</summary>
    Pain001,

    /// <summary>Customer payment status report (<c>pain.002</c>).</summary>
    Pain002,

    /// <summary>Customer direct debit initiation (<c>pain.008</c>).</summary>
    Pain008,
}
