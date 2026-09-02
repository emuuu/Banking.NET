using Commerzbank.NET.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.DependencyInjection;

public class CommerzbankServiceCollectionExtensionsTests
{
    private static IServiceCollection ValidServices(Action<CommerzbankOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
            configureOptions?.Invoke(options);
        });
        return services;
    }

    [Fact]
    public void AddCommerzbank_WithAction_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
        });

        builder.ShouldNotBeNull();
        builder.Services.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddCommerzbank_WithConfiguration_ReturnsBuilder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerzbank:ClientId"] = "client-id",
                ["Commerzbank:ClientSecret"] = "client-secret",
            })
            .Build();

        var services = new ServiceCollection();
        var builder = services.AddCommerzbank(configuration.GetSection(CommerzbankOptions.SectionName));

        builder.ShouldNotBeNull();
    }

    [Fact]
    public void AddCommerzbank_WithConfiguration_BindsOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerzbank:ClientId"] = "config-client-id",
                ["Commerzbank:ClientSecret"] = "config-client-secret",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCommerzbank(configuration.GetSection(CommerzbankOptions.SectionName));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CommerzbankOptions>>().Value;
        options.ClientId.ShouldBe("config-client-id");
        options.ClientSecret.ShouldBe("config-client-secret");
    }

    [Fact]
    public void AddCommerzbank_WithoutAccessTokenProvider_RegistersClientCredentialsTokenProvider()
    {
        using var provider = ValidServices().BuildServiceProvider();
        provider.GetRequiredService<IAccessTokenProvider>().ShouldBeOfType<ClientCredentialsTokenProvider>();
    }

    [Fact]
    public void AddCommerzbank_WithAccessTokenProvider_RegistersDelegateAccessTokenProvider()
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(options => options.AccessTokenProvider = _ => ValueTask.FromResult("token"));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAccessTokenProvider>().ShouldBeOfType<DelegateAccessTokenProvider>();
    }

    [Fact]
    public void AddCommerzbank_ApiClient_HasBaseAddressTimeoutAndClientProductHeader()
    {
        var services = ValidServices(options =>
        {
            options.Environment = CommerzbankEnvironment.Sandbox;
            options.Timeout = TimeSpan.FromSeconds(42);
            options.ClientProduct = "MyErp/2.4";
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CommerzbankHttpClientNames.Api);

        client.BaseAddress.ShouldBe(new Uri("https://api-sandbox.commerzbank.com/"));
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(42));
        client.DefaultRequestHeaders.GetValues("ClientProduct").ShouldContain("MyErp/2.4");
    }

    [Fact]
    public void AddCommerzbank_ApiClient_WithoutClientProduct_HasNoClientProductHeader()
    {
        using var provider = ValidServices().BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CommerzbankHttpClientNames.Api);

        client.DefaultRequestHeaders.Contains("ClientProduct").ShouldBeFalse();
    }

    [Fact]
    public void AddCommerzbank_TokenClient_HasTokenTimeout()
    {
        var services = ValidServices(options => options.TokenTimeout = TimeSpan.FromSeconds(7));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CommerzbankHttpClientNames.Token);

        client.Timeout.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void AddCommerzbank_WithInvalidOptions_ResolvingOptionsThrowsOptionsValidationException()
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(_ => { });

        using var provider = services.BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CommerzbankOptions>>().Value);
    }

    [Fact]
    public void AddCommerzbank_CalledTwice_RegistersAccessTokenProviderOnce()
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
        });
        services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
        });

        services.Count(descriptor => descriptor.ServiceType == typeof(IAccessTokenProvider)).ShouldBe(1);
    }

    [Fact]
    public void AddCommerzbank_CalledTwiceWithClientProduct_ClientProductHeaderHasExactlyOneValue()
    {
        var services = new ServiceCollection();
        services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
            options.ClientProduct = "MyErp/2.4";
        });
        services.AddCommerzbank(options =>
        {
            options.ClientId = "client-id";
            options.ClientSecret = "client-secret";
            options.ClientProduct = "MyErp/2.4";
        });

        services.Count(descriptor => descriptor.ServiceType == typeof(IAccessTokenProvider)).ShouldBe(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(ClientCertificateProvider)).ShouldBe(1);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(CommerzbankHttpClientNames.Api);

        client.DefaultRequestHeaders.GetValues("ClientProduct").ShouldBe(["MyErp/2.4"]);
    }

    [Fact]
    public void CreatePrimaryHandler_WithConfiguredCertificate_AddsCertificateToHandler()
    {
        using var certificate = Helpers.TestCertificates.CreateSelfSigned();
        var services = ValidServices(options => options.ClientCertificate = certificate);

        using var provider = services.BuildServiceProvider();
        using var handler = (HttpClientHandler)CommerzbankServiceCollectionExtensions.CreatePrimaryHandler(provider);

        handler.ClientCertificateOptions.ShouldBe(ClientCertificateOption.Manual);
        handler.ClientCertificates.Cast<System.Security.Cryptography.X509Certificates.X509Certificate2>().ShouldContain(certificate);
    }

    [Fact]
    public void CreatePrimaryHandler_WithoutCertificate_AddsNoCertificates()
    {
        using var provider = ValidServices().BuildServiceProvider();
        using var handler = (HttpClientHandler)CommerzbankServiceCollectionExtensions.CreatePrimaryHandler(provider);

        handler.ClientCertificates.Count.ShouldBe(0);
    }

    [Fact]
    public void AddCommerzbank_RegistersAuthHandlerAsTransient()
    {
        var services = ValidServices();
        var descriptor = services.SingleOrDefault(sd => sd.ServiceType == typeof(CommerzbankAuthHandler));

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }
}
