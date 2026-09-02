using System.Security.Cryptography;
using Commerzbank.NET.Auth;
using Commerzbank.NET.Tests.Helpers;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Auth;

public class ClientCertificateProviderTests
{
    [Fact]
    public void Certificate_WithClientCertificateObject_ReturnsSameInstance()
    {
        using var certificate = TestCertificates.CreateSelfSigned();
        var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions { ClientCertificate = certificate });
        using var provider = new ClientCertificateProvider(options);

        provider.Certificate.ShouldBeSameAs(certificate);
    }

    [Fact]
    public void Dispose_WithClientCertificateObject_DoesNotDisposeIt()
    {
        using var certificate = TestCertificates.CreateSelfSigned();
        var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions { ClientCertificate = certificate });
        var provider = new ClientCertificateProvider(options);

        _ = provider.Certificate;
        provider.Dispose();

        Should.NotThrow(() => certificate.Thumbprint);
    }

    [Fact]
    public void Certificate_WithClientCertificatePath_LoadsFromFile()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var path = TestCertificates.WritePkcs12(original);
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions { ClientCertificatePath = path });
            using var provider = new ClientCertificateProvider(options);

            provider.Certificate.ShouldNotBeNull();
            provider.Certificate!.Thumbprint.ShouldBe(original.Thumbprint);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_WithClientCertificatePath_DisposesLoadedCertificate()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var path = TestCertificates.WritePkcs12(original);
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions { ClientCertificatePath = path });
            var provider = new ClientCertificateProvider(options);
            var certificate = provider.Certificate;
            provider.Dispose();

            Should.Throw<CryptographicException>(() => certificate!.Thumbprint);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Certificate_WithNeitherObjectNorPath_IsNull()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions());
        using var provider = new ClientCertificateProvider(options);

        provider.Certificate.ShouldBeNull();
    }

    [Fact]
    public void Dispose_WithNeitherObjectNorPath_DoesNotThrow()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions());
        var provider = new ClientCertificateProvider(options);

        Should.NotThrow(provider.Dispose);
    }

    [Fact]
    public void Dispose_WithoutPriorCertificateAccess_DoesNotLoadCertificate()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.p12");
        var options = Microsoft.Extensions.Options.Options.Create(new CommerzbankOptions { ClientCertificatePath = missingPath });
        var provider = new ClientCertificateProvider(options);

        Should.NotThrow(provider.Dispose);
    }
}
