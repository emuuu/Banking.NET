namespace Commerzbank.NET.CorporatePayments;

/// <summary>The confirmation status sent to <see cref="ICorporatePaymentsClient.ConfirmMessageAsync"/>.</summary>
public enum ReceivedStatus
{
    /// <summary>Only part of the message was received; the bank keeps delivering the remaining fragments.</summary>
    Partial,

    /// <summary>The message was received in full; the bank stops redelivering it.</summary>
    Complete,
}
