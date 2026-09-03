namespace Banking.NET.Docs.Services;

/// <summary>Holds the sandbox client ID and secret entered by the visitor for the duration of the browser session.</summary>
public sealed class DocsCredentialService
{
    private string _clientId = "";
    private string _clientSecret = "";

    public string ClientId => _clientId;
    public string ClientSecret => _clientSecret;
    public bool HasCredentials => !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);

    public event Action? CredentialsChanged;

    public void UpdateCredentials(string clientId, string clientSecret)
    {
        _clientId = clientId.Trim();
        _clientSecret = clientSecret.Trim();
        CredentialsChanged?.Invoke();
    }
}
