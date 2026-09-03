using System.Security.Cryptography.X509Certificates;

namespace Banking.NET.Commerzbank.Auth;

/// <summary>Loads client certificates used for mutual TLS against the Commerzbank production gateway.</summary>
public static class ClientCertificateLoader
{
    /// <summary>Loads a certificate and private key from a PKCS#12 (.pfx/.p12) file.</summary>
    /// <param name="path">The path to the PKCS#12 file.</param>
    /// <param name="password">The password protecting the file, if any.</param>
    /// <returns>The loaded certificate.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public static X509Certificate2 LoadPkcs12(string path, string? password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Client certificate file not found: '{path}'.", path);

        return X509CertificateLoader.LoadPkcs12FromFile(path, password);
    }

    /// <summary>Loads a certificate and private key from a PEM certificate and a separate PEM private key file.</summary>
    /// <param name="certificatePath">The path to the PEM certificate file.</param>
    /// <param name="keyPath">The path to the PEM private key file.</param>
    /// <param name="keyPassword">The password protecting the private key, if any.</param>
    /// <returns>The loaded certificate.</returns>
    /// <exception cref="FileNotFoundException">The certificate or key file does not exist.</exception>
    public static X509Certificate2 LoadPem(string certificatePath, string keyPath, string? keyPassword = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);
        if (!File.Exists(certificatePath))
            throw new FileNotFoundException($"Client certificate file not found: '{certificatePath}'.", certificatePath);
        if (!File.Exists(keyPath))
            throw new FileNotFoundException($"Client certificate key file not found: '{keyPath}'.", keyPath);

        var certificate = keyPassword is null
            ? X509Certificate2.CreateFromPemFile(certificatePath, keyPath)
            : X509Certificate2.CreateFromEncryptedPemFile(certificatePath, keyPassword, keyPath);

        // SChannel does not use ephemeral keys for TLS client authentication, so a PEM-loaded certificate must be re-imported as PKCS#12 on Windows.
        if (!OperatingSystem.IsWindows())
            return certificate;

        using (certificate)
        {
            return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), (string?)null);
        }
    }

    /// <summary>Returns the configured certificate or null. Loads from file when <see cref="CommerzbankOptions.ClientCertificatePath"/> is set (PEM when <see cref="CommerzbankOptions.ClientCertificateKeyPath"/> is set, otherwise PKCS#12).</summary>
    /// <param name="options">The options to read the certificate configuration from.</param>
    /// <returns>The configured client certificate, or null if none is configured.</returns>
    public static X509Certificate2? Load(CommerzbankOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ClientCertificate is not null)
            return options.ClientCertificate;

        if (options.ClientCertificatePath is null)
            return null;

        return options.ClientCertificateKeyPath is not null
            ? LoadPem(options.ClientCertificatePath, options.ClientCertificateKeyPath, options.ClientCertificatePassword)
            : LoadPkcs12(options.ClientCertificatePath, options.ClientCertificatePassword);
    }
}
