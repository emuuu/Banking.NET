using Commerzbank.NET.CorporatePayments;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.DependencyInjection;

public class CorporatePaymentsBuilderExtensionsTests
{
    private static ICommerzbankBuilder ValidBuilder()
    {
        var services = new ServiceCollection();
        return services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
        });
    }

    [Fact]
    public void AddCorporatePayments_RegistersResolvableClient()
    {
        var builder = ValidBuilder();
        builder.AddCorporatePayments();

        using var provider = builder.Services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICorporatePaymentsClient>();

        client.ShouldBeOfType<CorporatePaymentsClient>();
    }

    [Fact]
    public void AddCorporatePayments_ClientUsesSandboxBaseAddress()
    {
        var builder = ValidBuilder();
        builder.AddCorporatePayments();

        using var provider = builder.Services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CommerzbankHttpClientNames.Api);

        client.BaseAddress.ShouldBe(new Uri("https://api-sandbox.commerzbank.com/"));
    }

    [Fact]
    public void AddCorporatePayments_ReturnsSameBuilderForChaining()
    {
        var builder = ValidBuilder();

        var result = builder.AddCorporatePayments();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddCorporatePayments_NullBuilder_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => CorporatePaymentsBuilderExtensions.AddCorporatePayments(null!));
    }
}
