using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Banking.NET.Tests.Helpers;

/// <summary>Creates self-signed RSA test certificates at runtime and writes them to temporary files. No certificate files are ever committed to the repository.</summary>
internal static class TestCertificates
{
    public static X509Certificate2 CreateSelfSigned(string subjectName = "CN=Banking.NET Tests")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), (string?)null, X509KeyStorageFlags.Exportable);
    }

    public static string WritePkcs12(X509Certificate2 certificate, string? password = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.p12");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
        return path;
    }

    public static (string CertificatePath, string KeyPath) WritePem(X509Certificate2 certificate)
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.crt.pem");
        var keyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.key.pem");

        File.WriteAllText(certificatePath, certificate.ExportCertificatePem());

        using var rsa = certificate.GetRSAPrivateKey() ?? throw new InvalidOperationException("Certificate has no RSA private key.");
        File.WriteAllText(keyPath, rsa.ExportRSAPrivateKeyPem());

        return (certificatePath, keyPath);
    }
}
