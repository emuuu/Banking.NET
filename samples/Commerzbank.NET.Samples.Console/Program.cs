// Corporate Payments sandbox walkthrough: heartbeat, list waiting messages, download and inspect
// each one, then optionally submit a sample SEPA credit transfer.
//
// Usage:
//   dotnet run --project samples/Commerzbank.NET.Samples.Console
//   dotnet run --project samples/Commerzbank.NET.Samples.Console -- --confirm --submit-sample

using Commerzbank.NET;
using Commerzbank.NET.CorporatePayments;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var clientId = configuration["COMMERZBANK_SANDBOX_CLIENT_ID"];
var clientSecret = configuration["COMMERZBANK_SANDBOX_CLIENT_SECRET"];

if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
{
    Console.Error.WriteLine("Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run this sample against the sandbox.");
    Console.Error.WriteLine("Sandbox credentials are issued through a project in the Commerzbank Developer Portal.");
    return 1;
}

// Nothing is confirmed or submitted unless explicitly requested, so the sample is safe to run repeatedly.
var confirm = args.Contains("--confirm");
var submitSample = args.Contains("--submit-sample");

var services = new ServiceCollection();
services.AddCommerzbank(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.Environment = CommerzbankEnvironment.Sandbox;
}).AddCorporatePayments();

using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<ICorporatePaymentsClient>();

try
{
    await client.HeartbeatAsync();
    Console.WriteLine("Heartbeat OK.");

    var messages = await client.ListMessagesAsync();
    Console.WriteLine();
    Console.WriteLine($"{messages.Count} message(s) waiting:");
    Console.WriteLine($"{"MessageId",-36} {"OrderType",-9} {"Fragments",-9} Size");
    foreach (var info in messages)
        Console.WriteLine($"{info.MessageId,-36} {info.OrderType.Code,-9} {info.Fragments,-9} {info.Size}");

    foreach (var info in messages)
    {
        var message = await client.DownloadMessageAsync(info);
        var identifier = Iso20022Document.Identify(Iso20022Document.Load(message.OpenContentStream()));
        Console.WriteLine();
        Console.WriteLine($"{info.MessageId} ({identifier.Identifier}):");

        // camt.052/053/054 and pain.002 are the message families this sample knows how to read;
        // any other order type is only shown by its identified schema.
        switch (identifier.Type)
        {
            case Iso20022MessageType.Camt053:
            case Iso20022MessageType.Camt052:
            case Iso20022MessageType.Camt054:
                var bankToCustomerMessage = CamtReader.Read(message);
                foreach (var statement in bankToCustomerMessage.Statements)
                    Console.WriteLine($"  Account {statement.Account?.Iban}: {statement.Balances.Count} balance(s), {statement.Entries.Count} entrie(s)");
                break;

            case Iso20022MessageType.Pain002:
                var report = Pain002Reader.Read(message);
                Console.WriteLine($"  Group status: {report.OriginalGroup.GroupStatus}");
                break;

            default:
                Console.WriteLine($"  (no reader wired up for {identifier.Type} in this sample)");
                break;
        }

        // Confirmation stops the bank from redelivering the message; leaving it unconfirmed is
        // the safe default for a sample that may be run more than once.
        if (confirm)
        {
            await client.ConfirmMessageAsync(info.MessageId);
            Console.WriteLine("  Confirmed.");
        }
    }

    if (submitSample)
    {
        Console.WriteLine();
        var initiation = BuildSampleCreditTransfer();
        var xml = Pain001Writer.WriteToString(initiation, Pain001Version.V09);
        var result = await client.SubmitOrderAsync(OrderType.CCT, xml);
        Console.WriteLine($"Submitted sample CCT order: {result.StatusCode}");
    }
}
catch (CommerzbankException ex)
{
    Console.Error.WriteLine($"API error: {ex.Message} (status {ex.StatusCode}, correlation {ex.CorrelationId})");
    return 1;
}

return 0;

// Fictional data only (no real account or customer information), matching the test IBANs used throughout the test suite.
static CreditTransferInitiation BuildSampleCreditTransfer() => new()
{
    MessageId = $"SAMPLE-{Guid.NewGuid():N}",
    InitiatingParty = new PartyIdentification { Name = "Example Debtor GmbH" },
    PaymentInformations =
    [
        new CreditTransferPaymentInformation
        {
            PaymentInformationId = "PMTINF-0001",
            RequestedExecutionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Debtor = new PartyIdentification { Name = "Example Debtor GmbH" },
            DebtorAccount = new AccountIdentification { Iban = "DE89370400440532013000" },
            Transactions =
            [
                new CreditTransferTransaction
                {
                    EndToEndId = "E2E-0001",
                    Amount = new Money(100.00m, "EUR"),
                    Creditor = new PartyIdentification { Name = "Example Creditor Ltd" },
                    CreditorAccount = new AccountIdentification { Iban = "DE02120300000000202051" },
                },
            ],
        },
    ],
};
