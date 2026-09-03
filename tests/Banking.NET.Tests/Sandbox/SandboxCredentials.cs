namespace Banking.NET.Tests.Sandbox;

/// <summary>Sandbox OAuth credentials and confirm opt-in read from the environment. Integration tests skip unless both credentials are set.</summary>
internal static class SandboxCredentials
{
    public static string? ClientId { get; } = Environment.GetEnvironmentVariable("COMMERZBANK_SANDBOX_CLIENT_ID");

    public static string? ClientSecret { get; } = Environment.GetEnvironmentVariable("COMMERZBANK_SANDBOX_CLIENT_SECRET");

    public static bool Available => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>
    /// Whether tests that confirm sandbox messages (and thereby permanently stop their redelivery) are allowed to
    /// run. Requires both <see cref="Available"/> and the explicit opt-in environment variable
    /// <c>COMMERZBANK_SANDBOX_ALLOW_CONFIRM</c> (value <c>1</c> or <c>true</c>); false by default in CI and locally.
    /// </summary>
    public static bool ConfirmAllowed => Available && Environment.GetEnvironmentVariable("COMMERZBANK_SANDBOX_ALLOW_CONFIRM") is "1" or "true";
}
