using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A balance reported on a billing statement (<c>Bal</c>).</summary>
public sealed class BillingBalance
{
    /// <summary>The coded or proprietary balance type (<c>Tp/Cd</c> or <c>Tp/Prtry</c>).</summary>
    public string? TypeCode { get; init; }

    /// <summary>The balance amount, unsigned (<c>Val/Amt</c>).</summary>
    public required Money Amount { get; init; }

    /// <summary>Whether the balance is a credit or a debit balance, derived from the sign of the balance value (<c>Val/Sgn</c>: <see langword="true"/> means debit, <see langword="false"/> or absent means credit).</summary>
    public CreditDebitIndicator? CreditDebit { get; init; }

    /// <summary>The <c>Bal</c> element this balance was read from.</summary>
    public required XElement Source { get; init; }
}
