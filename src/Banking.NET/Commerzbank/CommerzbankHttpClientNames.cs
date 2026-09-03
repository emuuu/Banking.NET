namespace Banking.NET.Commerzbank;

/// <summary>Names of the named <see cref="System.Net.Http.HttpClient"/> instances registered by <see cref="CommerzbankServiceCollectionExtensions"/>.</summary>
public static class CommerzbankHttpClientNames
{
    /// <summary>The named client used for Commerzbank Corporate Payments API requests.</summary>
    public const string Api = "Commerzbank.Api";

    /// <summary>The named client used for OAuth token requests.</summary>
    public const string Token = "Commerzbank.Token";
}
