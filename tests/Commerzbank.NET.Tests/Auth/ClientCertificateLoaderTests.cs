using Commerzbank.NET.Auth;
using Commerzbank.NET.Tests.Helpers;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Auth;

public class ClientCertificateLoaderTests
{
    [Fact]
    public void LoadPkcs12_FromTempFile_ReturnsCertificateWithPrivateKey()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var path = TestCertificates.WritePkcs12(original, "test-password");
        try
        {
            using var loaded = ClientCertificateLoader.LoadPkcs12(path, "test-password");
            loaded.Thumbprint.ShouldBe(original.Thumbprint);
            loaded.HasPrivateKey.ShouldBeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadPem_FromTempCertAndKeyFiles_ReturnsCertificateWithPrivateKey()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var (certPath, keyPath) = TestCertificates.WritePem(original);
        try
        {
            using var loaded = ClientCertificateLoader.LoadPem(certPath, keyPath);
            loaded.Thumbprint.ShouldBe(original.Thumbprint);
            loaded.HasPrivateKey.ShouldBeTrue();
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public void LoadPkcs12_MissingFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.p12");
        Should.Throw<FileNotFoundException>(() => ClientCertificateLoader.LoadPkcs12(path));
    }

    [Fact]
    public void LoadPem_MissingCertificateFile_ThrowsFileNotFoundException()
    {
        var certPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.crt.pem");
        var keyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.key.pem");
        Should.Throw<FileNotFoundException>(() => ClientCertificateLoader.LoadPem(certPath, keyPath));
    }

    [Fact]
    public void LoadPem_MissingKeyFile_ThrowsFileNotFoundException()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var (certPath, keyPath) = TestCertificates.WritePem(original);
        var missingKeyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.key.pem");
        try
        {
            Should.Throw<FileNotFoundException>(() => ClientCertificateLoader.LoadPem(certPath, missingKeyPath));
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public void Load_WithClientCertificateObject_ReturnsThatObject()
    {
        using var certificate = TestCertificates.CreateSelfSigned();
        var options = new CommerzbankOptions { ClientCertificate = certificate };

        ClientCertificateLoader.Load(options).ShouldBeSameAs(certificate);
    }

    [Fact]
    public void Load_WithPkcs12Path_LoadsFromFile()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var path = TestCertificates.WritePkcs12(original);
        try
        {
            var options = new CommerzbankOptions { ClientCertificatePath = path };
            using var loaded = ClientCertificateLoader.Load(options);
            loaded.ShouldNotBeNull();
            loaded!.Thumbprint.ShouldBe(original.Thumbprint);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WithPemPaths_LoadsFromFiles()
    {
        using var original = TestCertificates.CreateSelfSigned();
        var (certPath, keyPath) = TestCertificates.WritePem(original);
        try
        {
            var options = new CommerzbankOptions { ClientCertificatePath = certPath, ClientCertificateKeyPath = keyPath };
            using var loaded = ClientCertificateLoader.Load(options);
            loaded.ShouldNotBeNull();
            loaded!.Thumbprint.ShouldBe(original.Thumbprint);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public void Load_WithoutCertificateConfigured_ReturnsNull()
    {
        var options = new CommerzbankOptions();
        ClientCertificateLoader.Load(options).ShouldBeNull();
    }
}
