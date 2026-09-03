using Banking.NET.Commerzbank;
using Banking.NET.Commerzbank.CorporatePayments;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>Builds a service provider wired against the sandbox environment. Options validation only happens when a service is actually resolved, so building this fixture is safe even without credentials — only the skipped tests would fail to resolve.</summary>
public sealed class SandboxFixture : IAsyncLifetime
{
    private ServiceProvider? _services;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("The fixture has not been initialized yet.");

    public ICorporatePaymentsClient Client => Services.GetRequiredService<ICorporatePaymentsClient>();

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(options =>
        {
            options.ClientId = SandboxCredentials.ClientId;
            options.ClientSecret = SandboxCredentials.ClientSecret;
            options.Environment = CommerzbankEnvironment.Sandbox;
            options.ClientProduct = "Banking.NET.Tests/1.0";
        }).AddCorporatePayments();

        _services = services.BuildServiceProvider();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _services?.Dispose();
        return ValueTask.CompletedTask;
    }
}
