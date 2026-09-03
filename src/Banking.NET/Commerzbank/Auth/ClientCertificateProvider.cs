using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace Banking.NET.Commerzbank.Auth;

internal sealed class ClientCertificateProvider : IDisposable
{
    private readonly Lazy<LoadedCertificate> _certificate;

    public ClientCertificateProvider(IOptions<CommerzbankOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _certificate = new Lazy<LoadedCertificate>(() =>
        {
            var value = options.Value;
            var owned = value.ClientCertificate is null && value.ClientCertificatePath is not null;
            return new LoadedCertificate(ClientCertificateLoader.Load(value), owned);
        });
    }

    public X509Certificate2? Certificate => _certificate.Value.Certificate;

    public void Dispose()
    {
        if (_certificate.IsValueCreated && _certificate.Value is { Owned: true, Certificate: { } certificate })
            certificate.Dispose();
    }

    private sealed record LoadedCertificate(X509Certificate2? Certificate, bool Owned);
}
