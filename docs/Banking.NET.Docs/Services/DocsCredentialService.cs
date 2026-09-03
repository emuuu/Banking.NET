namespace Banking.NET.Docs.Services;

/// <summary>Holds the sandbox client ID and secret entered by the visitor for the duration of the browser session.</summary>
public sealed class DocsCredentialService
{
    private string _clientId = "";
    private string _clientSecret = "";

    /// <summary>The sandbox client ID entered for this session, or empty if none has been entered.</summary>
    public string ClientId => _clientId;

    /// <summary>The sandbox client secret entered for this session, or empty if none has been entered.</summary>
    public string ClientSecret => _clientSecret;

    /// <summary>Whether both a client ID and a client secret have been entered.</summary>
    public bool HasCredentials => !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);

    /// <summary>Raised whenever <see cref="UpdateCredentials"/> is called.</summary>
    public event Action? CredentialsChanged;

    /// <summary>Stores a new client ID/secret pair, trimmed of surrounding whitespace, and raises <see cref="CredentialsChanged"/>.</summary>
    /// <param name="clientId">The sandbox client ID.</param>
    /// <param name="clientSecret">The sandbox client secret.</param>
    public void UpdateCredentials(string clientId, string clientSecret)
    {
        _clientId = clientId.Trim();
        _clientSecret = clientSecret.Trim();
        CredentialsChanged?.Invoke();
    }
}
