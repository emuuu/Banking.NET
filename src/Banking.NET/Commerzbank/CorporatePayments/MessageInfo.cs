namespace Banking.NET.Commerzbank.CorporatePayments;

/// <summary>An entry returned by <see cref="ICorporatePaymentsClient.ListMessagesAsync"/> describing a message waiting to be downloaded.</summary>
/// <param name="MessageId">The identifier used to download or confirm the message.</param>
/// <param name="OrderType">The order type of the message.</param>
/// <param name="Fragments">The number of fragments the message is split into.</param>
/// <param name="Size">The total size of the message in bytes.</param>
public sealed record MessageInfo(string MessageId, OrderType OrderType, int Fragments, long Size);
