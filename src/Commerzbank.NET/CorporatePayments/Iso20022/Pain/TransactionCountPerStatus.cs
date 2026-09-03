namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>A count of transactions sharing a detailed status, with an optional control sum (ISO 20022 <c>NumberOfTransactionsPerStatus</c>).</summary>
/// <param name="Status">The detailed transaction status code (<c>DtldSts</c>).</param>
/// <param name="Count">The number of transactions with this status (<c>DtldNbOfTxs</c>).</param>
/// <param name="ControlSum">The total of the amounts of all transactions with this status (<c>DtldCtrlSum</c>).</param>
public sealed record TransactionCountPerStatus(string Status, int Count, decimal? ControlSum);
