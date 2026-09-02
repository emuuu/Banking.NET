namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>An amount together with its ISO 4217 currency code.</summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The ISO 4217 currency code (e.g. "EUR"). Empty when the source element carries no <c>Ccy</c> attribute and no fallback currency (e.g. the surrounding account's currency) is available.</param>
public readonly record struct Money(decimal Amount, string Currency);
