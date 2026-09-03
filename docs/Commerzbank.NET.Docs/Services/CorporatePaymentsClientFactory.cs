using Commerzbank.NET.Auth;
using Commerzbank.NET.CorporatePayments;

namespace Commerzbank.NET.Docs.Services;

/// <summary>
/// Builds an <see cref="ICorporatePaymentsClient"/> against the sandbox from the credentials entered in the browser.
/// The auth handler chain captures the client ID/secret at construction time, so a changed credential pair
/// requires a brand-new client rather than a mutation of the existing one.
/// </summary>
public sealed class CorporatePaymentsClientFactory : IDisposable
{
    private readonly DocsCredentialService _credentials;
    private ICorporatePaymentsClient? _client;
    private ClientCredentialsTokenProvider? _tokenProvider;
    private HttpClient? _tokenHttpClient;
    private HttpClient? _apiHttpClient;

    public CorporatePaymentsClientFactory(DocsCredentialService credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _credentials = credentials;
        _credentials.CredentialsChanged += OnCredentialsChanged;
    }

    /// <summary>The current client, or null when no sandbox credentials have been entered yet.</summary>
    public ICorporatePaymentsClient? Client => _client ??= CreateClient();

    private void OnCredentialsChanged() => DisposeClient();

    private ICorporatePaymentsClient? CreateClient()
    {
        if (!_credentials.HasCredentials)
            return null;

        var options = new CommerzbankOptions
        {
            ClientId = _credentials.ClientId,
            ClientSecret = _credentials.ClientSecret,
            Environment = CommerzbankEnvironment.Sandbox,
            ClientProduct = "Commerzbank.NET.Docs/1.0"
        };

        _tokenHttpClient = new HttpClient { Timeout = options.TokenTimeout };
        _tokenProvider = new ClientCredentialsTokenProvider(_tokenHttpClient, options);

        var authHandler = new CommerzbankAuthHandler(_tokenProvider) { InnerHandler = new HttpClientHandler() };
        _apiHttpClient = new HttpClient(authHandler)
        {
            BaseAddress = options.GetApiBaseUri(),
            Timeout = options.Timeout
        };
        _apiHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("ClientProduct", options.ClientProduct);

        return new CorporatePaymentsClient(_apiHttpClient);
    }

    private void DisposeClient()
    {
        _client = null;
        _apiHttpClient?.Dispose();
        _apiHttpClient = null;
        _tokenProvider?.Dispose();
        _tokenProvider = null;
        _tokenHttpClient?.Dispose();
        _tokenHttpClient = null;
    }

    public void Dispose()
    {
        _credentials.CredentialsChanged -= OnCredentialsChanged;
        DisposeClient();
    }
}
