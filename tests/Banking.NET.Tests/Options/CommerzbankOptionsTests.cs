using Banking.NET.Commerzbank;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Options;

public class CommerzbankOptionsTests
{
    private static CommerzbankOptions ValidOptions() => new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
    };

    // V1
    [Fact]
    public void Validate_WithClientIdAndSecret_DoesNotThrow()
    {
        var options = ValidOptions();
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void Validate_WithAccessTokenProviderAndNoClientCredentials_DoesNotThrow()
    {
        var options = new CommerzbankOptions { AccessTokenProvider = (_) => ValueTask.FromResult("token") };
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void Validate_WithoutClientCredentialsAndNoAccessTokenProvider_Throws()
    {
        var options = new CommerzbankOptions();
        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientId));
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientSecret));
    }

    // V2
    [Fact]
    public void Validate_WithOnlyClientCertificate_DoesNotThrow()
    {
        using var certificate = Helpers.TestCertificates.CreateSelfSigned();
        var options = ValidOptions();
        options.ClientCertificate = certificate;
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void Validate_WithClientCertificateAndClientCertificatePathBothSet_Throws()
    {
        using var certificate = Helpers.TestCertificates.CreateSelfSigned();
        var options = ValidOptions();
        options.ClientCertificate = certificate;
        options.ClientCertificatePath = "/tmp/cert.p12";

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientCertificate));
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientCertificatePath));
    }

    // V3
    [Fact]
    public void Validate_WithClientCertificateKeyPathAndCertificatePath_DoesNotThrow()
    {
        var options = ValidOptions();
        options.ClientCertificatePath = "/tmp/cert.pem";
        options.ClientCertificateKeyPath = "/tmp/key.pem";
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void Validate_WithClientCertificateKeyPathWithoutCertificatePath_Throws()
    {
        var options = ValidOptions();
        options.ClientCertificateKeyPath = "/tmp/key.pem";

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientCertificateKeyPath));
    }

    // V4
    [Fact]
    public void Validate_ProductionWithoutCertificateOrApiBaseUrl_Throws()
    {
        var options = ValidOptions();
        options.Environment = CommerzbankEnvironment.Production;

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientCertificate));
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ClientCertificatePath));
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ApiBaseUrl));
    }

    [Fact]
    public void Validate_ProductionWithClientCertificatePath_DoesNotThrow()
    {
        var options = ValidOptions();
        options.Environment = CommerzbankEnvironment.Production;
        options.ClientCertificatePath = "/tmp/cert.p12";
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void Validate_ProductionWithApiBaseUrlOverride_DoesNotThrow()
    {
        var options = ValidOptions();
        options.Environment = CommerzbankEnvironment.Production;
        options.ApiBaseUrl = "https://proxy.example.com/commerzbank/";
        Should.NotThrow(options.Validate);
    }

    // V5
    [Fact]
    public void Validate_WithRelativeApiBaseUrl_Throws()
    {
        var options = ValidOptions();
        options.ApiBaseUrl = "not-a-url";

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.ApiBaseUrl));
    }

    [Fact]
    public void Validate_WithNonHttpTokenEndpointScheme_Throws()
    {
        var options = ValidOptions();
        options.TokenEndpoint = "ftp://example.com/token";

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.TokenEndpoint));
    }

    [Fact]
    public void Validate_WithValidAbsoluteApiBaseUrlAndTokenEndpoint_DoesNotThrow()
    {
        var options = ValidOptions();
        options.ApiBaseUrl = "https://proxy.example.com/";
        options.TokenEndpoint = "https://proxy.example.com/token";
        Should.NotThrow(options.Validate);
    }

    // V6
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveTimeout_Throws(int seconds)
    {
        var options = ValidOptions();
        options.Timeout = TimeSpan.FromSeconds(seconds);

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.Timeout));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveTokenTimeout_Throws(int seconds)
    {
        var options = ValidOptions();
        options.TokenTimeout = TimeSpan.FromSeconds(seconds);

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.TokenTimeout));
    }

    [Fact]
    public void Validate_WithNegativeTokenExpiryMargin_Throws()
    {
        var options = ValidOptions();
        options.TokenExpiryMargin = TimeSpan.FromSeconds(-1);

        var exception = Should.Throw<InvalidOperationException>(options.Validate);
        exception.Message.ShouldContain(nameof(CommerzbankOptions.TokenExpiryMargin));
    }

    [Fact]
    public void Validate_WithZeroTokenExpiryMargin_DoesNotThrow()
    {
        var options = ValidOptions();
        options.TokenExpiryMargin = TimeSpan.Zero;
        Should.NotThrow(options.Validate);
    }

    // GetApiBaseUri / GetTokenEndpointUri
    [Fact]
    public void GetApiBaseUri_Sandbox_ReturnsSandboxHost()
    {
        var options = new CommerzbankOptions { Environment = CommerzbankEnvironment.Sandbox };
        options.GetApiBaseUri().ShouldBe(new Uri("https://api-sandbox.commerzbank.com/"));
    }

    [Fact]
    public void GetApiBaseUri_Production_ReturnsProductionHost()
    {
        var options = new CommerzbankOptions { Environment = CommerzbankEnvironment.Production };
        options.GetApiBaseUri().ShouldBe(new Uri("https://api.commerzbank.com/"));
    }

    [Fact]
    public void GetApiBaseUri_WithOverrideMissingTrailingSlash_AppendsTrailingSlash()
    {
        var options = new CommerzbankOptions { ApiBaseUrl = "https://proxy.example.com/prefix" };
        options.GetApiBaseUri().ShouldBe(new Uri("https://proxy.example.com/prefix/"));
    }

    [Fact]
    public void GetApiBaseUri_WithOverrideHavingTrailingSlash_LeavesUnchanged()
    {
        var options = new CommerzbankOptions { ApiBaseUrl = "https://proxy.example.com/prefix/" };
        options.GetApiBaseUri().ShouldBe(new Uri("https://proxy.example.com/prefix/"));
    }

    [Fact]
    public void GetApiBaseUri_WithOverridePathPrefix_KeepsPrefix()
    {
        var options = new CommerzbankOptions { ApiBaseUrl = "https://proxy.example.com/corp/gateway" };
        options.GetApiBaseUri().ShouldBe(new Uri("https://proxy.example.com/corp/gateway/"));
    }

    [Fact]
    public void GetTokenEndpointUri_Sandbox_ReturnsSandboxRealm()
    {
        var options = new CommerzbankOptions { Environment = CommerzbankEnvironment.Sandbox };
        options.GetTokenEndpointUri().ShouldBe(new Uri("https://api-sandbox.commerzbank.com/auth/realms/sandbox/protocol/openid-connect/token"));
    }

    [Fact]
    public void GetTokenEndpointUri_Production_ReturnsExternalRealm()
    {
        var options = new CommerzbankOptions { Environment = CommerzbankEnvironment.Production };
        options.GetTokenEndpointUri().ShouldBe(new Uri("https://api.commerzbank.com/auth/realms/external/protocol/openid-connect/token"));
    }

    [Fact]
    public void GetTokenEndpointUri_WithOverride_ReturnsOverride()
    {
        var options = new CommerzbankOptions { TokenEndpoint = "https://proxy.example.com/token" };
        options.GetTokenEndpointUri().ShouldBe(new Uri("https://proxy.example.com/token"));
    }

    // Configuration binding
    [Fact]
    public void Binding_FromInMemoryConfiguration_BindsProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Commerzbank:ClientId"] = "config-client-id",
                ["Commerzbank:ClientSecret"] = "config-client-secret",
                ["Commerzbank:Environment"] = "Production",
                ["Commerzbank:ClientProduct"] = "MyErp/2.4",
            })
            .Build();

        var options = new CommerzbankOptions();
        configuration.GetSection(CommerzbankOptions.SectionName).Bind(options);

        options.ClientId.ShouldBe("config-client-id");
        options.ClientSecret.ShouldBe("config-client-secret");
        options.Environment.ShouldBe(CommerzbankEnvironment.Production);
        options.ClientProduct.ShouldBe("MyErp/2.4");
    }
}
