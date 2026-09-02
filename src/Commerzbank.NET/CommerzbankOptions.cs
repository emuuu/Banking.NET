using System.Security.Cryptography.X509Certificates;

namespace Commerzbank.NET;

/// <summary>Configures a Commerzbank Corporate Payments API client.</summary>
public sealed class CommerzbankOptions
{
    private const string SandboxApiBaseUrl = "https://api-sandbox.commerzbank.com/";
    private const string ProductionApiBaseUrl = "https://api.commerzbank.com/";
    private const string SandboxTokenEndpoint = "https://api-sandbox.commerzbank.com/auth/realms/sandbox/protocol/openid-connect/token";
    private const string ProductionTokenEndpoint = "https://api.commerzbank.com/auth/realms/external/protocol/openid-connect/token";

    /// <summary>The configuration section name used by <see cref="CommerzbankServiceCollectionExtensions"/>.</summary>
    public const string SectionName = "Commerzbank";

    /// <summary>The OAuth client ID. Required unless <see cref="AccessTokenProvider"/> is set.</summary>
    public string? ClientId { get; set; }

    /// <summary>The OAuth client secret. Required unless <see cref="AccessTokenProvider"/> is set.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>The API environment. Defaults to <see cref="CommerzbankEnvironment.Sandbox"/>.</summary>
    public CommerzbankEnvironment Environment { get; set; } = CommerzbankEnvironment.Sandbox;

    /// <summary>Absolute URL overriding the environment host, e.g. a reverse proxy or a test server. Any path is kept as a prefix; do not include the API path itself, the client appends it on its own.</summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>Absolute URL overriding the OAuth token endpoint.</summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>A client certificate to present for mutual TLS. Mutually exclusive with <see cref="ClientCertificatePath"/>.</summary>
    public X509Certificate2? ClientCertificate { get; set; }

    /// <summary>Path to a PKCS#12 (.pfx/.p12) or PEM certificate file. Mutually exclusive with <see cref="ClientCertificate"/>.</summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>Path to a PEM private key file; only used together with a PEM certificate in <see cref="ClientCertificatePath"/>.</summary>
    public string? ClientCertificateKeyPath { get; set; }

    /// <summary>Password protecting <see cref="ClientCertificatePath"/> or <see cref="ClientCertificateKeyPath"/>, if any.</summary>
    public string? ClientCertificatePassword { get; set; }

    /// <summary>External token source. When set, <see cref="ClientId"/> and <see cref="ClientSecret"/> are not required and no token requests are made by the library.</summary>
    public Func<CancellationToken, ValueTask<string>>? AccessTokenProvider { get; set; }

    /// <summary>Sent as the `ClientProduct` header on every API request when set (e.g. "MyErp/2.4").</summary>
    public string? ClientProduct { get; set; }

    /// <summary>The timeout for Corporate Payments API requests. Defaults to 100 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>The timeout for OAuth token requests. Defaults to 30 seconds.</summary>
    public TimeSpan TokenTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Whether to use a refresh token to renew an expiring access token before falling back to a new client credentials request. Defaults to true.</summary>
    public bool UseRefreshToken { get; set; } = true;

    /// <summary>The safety margin subtracted from an access token's expiry when deciding whether it is still usable. Defaults to 30 seconds.</summary>
    public TimeSpan TokenExpiryMargin { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets the API base URI: the sandbox or production host, or <see cref="ApiBaseUrl"/> when set. A trailing slash is guaranteed.</summary>
    /// <returns>The absolute base URI the Corporate Payments client appends its base path to.</returns>
    public Uri GetApiBaseUri()
    {
        var baseUrl = ApiBaseUrl ?? (Environment == CommerzbankEnvironment.Production ? ProductionApiBaseUrl : SandboxApiBaseUrl);
        return new Uri(EnsureTrailingSlash(baseUrl), UriKind.Absolute);
    }

    /// <summary>Gets the OAuth token endpoint URI: the sandbox or production realm, or <see cref="TokenEndpoint"/> when set.</summary>
    /// <returns>The absolute token endpoint URI.</returns>
    public Uri GetTokenEndpointUri()
    {
        var tokenEndpoint = TokenEndpoint ?? (Environment == CommerzbankEnvironment.Production ? ProductionTokenEndpoint : SandboxTokenEndpoint);
        return new Uri(tokenEndpoint, UriKind.Absolute);
    }

    /// <summary>Validates the option values. Throws <see cref="InvalidOperationException"/> with a message naming the offending property when invalid.</summary>
    internal void Validate()
    {
        if (AccessTokenProvider is null && (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret)))
            throw new InvalidOperationException($"{nameof(ClientId)} and {nameof(ClientSecret)} are required unless {nameof(AccessTokenProvider)} is set.");

        if (ClientCertificate is not null && ClientCertificatePath is not null)
            throw new InvalidOperationException($"{nameof(ClientCertificate)} and {nameof(ClientCertificatePath)} cannot both be set.");

        if (ClientCertificateKeyPath is not null && ClientCertificatePath is null)
            throw new InvalidOperationException($"{nameof(ClientCertificateKeyPath)} requires {nameof(ClientCertificatePath)} to be set.");

        if (Environment == CommerzbankEnvironment.Production && ApiBaseUrl is null && ClientCertificate is null && ClientCertificatePath is null)
            throw new InvalidOperationException($"Production requires a client certificate ({nameof(ClientCertificate)} or {nameof(ClientCertificatePath)}), unless {nameof(ApiBaseUrl)} overrides the gateway with a proxy that does not require mutual TLS.");

        ValidateAbsoluteUri(ApiBaseUrl, nameof(ApiBaseUrl));
        ValidateAbsoluteUri(TokenEndpoint, nameof(TokenEndpoint));

        if (Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(Timeout)} must be greater than zero.");

        if (TokenTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(TokenTimeout)} must be greater than zero.");

        if (TokenExpiryMargin < TimeSpan.Zero)
            throw new InvalidOperationException($"{nameof(TokenExpiryMargin)} must not be negative.");
    }

    private static void ValidateAbsoluteUri(string? value, string propertyName)
    {
        if (value is null)
            return;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException($"{propertyName} must be an absolute http or https URI.");
    }

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
}
