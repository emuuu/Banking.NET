namespace Banking.NET.Commerzbank;

/// <summary>Identifies which Commerzbank Corporate Payments API environment a client talks to.</summary>
public enum CommerzbankEnvironment
{
    /// <summary>The sandbox environment, serving mock data.</summary>
    Sandbox,

    /// <summary>The production environment. Requires a client certificate for mutual TLS.</summary>
    Production,
}
