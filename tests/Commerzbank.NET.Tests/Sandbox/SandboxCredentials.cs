namespace Commerzbank.NET.Tests.Sandbox;

/// <summary>Sandbox OAuth credentials read from the environment. Integration tests skip unless both are set.</summary>
internal static class SandboxCredentials
{
    public static string? ClientId { get; } = Environment.GetEnvironmentVariable("COMMERZBANK_SANDBOX_CLIENT_ID");

    public static string? ClientSecret { get; } = Environment.GetEnvironmentVariable("COMMERZBANK_SANDBOX_CLIENT_SECRET");

    public static bool Available => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
