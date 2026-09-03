using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>A balance reported for an account (<c>Bal</c>).</summary>
public sealed class Balance
{
    /// <summary>The coded balance type, e.g. <c>OPBD</c> or <c>CLBD</c> (<c>Tp/CdOrPrtry/Cd</c>).</summary>
    public string? TypeCode { get; init; }

    /// <summary>The proprietary balance type (<c>Tp/CdOrPrtry/Prtry</c>).</summary>
    public string? TypeProprietary { get; init; }

    /// <summary>The coded balance sub-type, e.g. <c>ITBD</c> (<c>Tp/SubTp/Cd</c>).</summary>
    public string? SubTypeCode { get; init; }

    /// <summary>The balance amount (<c>Amt</c>).</summary>
    public required Money Amount { get; init; }

    /// <summary>Whether the balance is a credit or a debit balance (<c>CdtDbtInd</c>).</summary>
    public required CreditDebitIndicator CreditDebit { get; init; }

    /// <summary>The balance date, without a time component (<c>Dt/Dt</c>).</summary>
    public DateOnly? Date { get; init; }

    /// <summary>The balance date and time (<c>Dt/DtTm</c>). Timestamps without an offset are read as UTC offset zero and are not converted to local time.</summary>
    public DateTimeOffset? DateTime { get; init; }

    /// <summary>The <c>Bal</c> element this balance was read from.</summary>
    public required XElement Source { get; init; }
}
