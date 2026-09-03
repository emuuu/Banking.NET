using Banking.NET.Commerzbank.Auth;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.Sandbox;

/// <summary>Verifies OAuth token acquisition and caching, and basic connectivity, against the live sandbox.</summary>
public sealed class SandboxAuthTests(SandboxFixture fixture, ITestOutputHelper output) : IClassFixture<SandboxFixture>
{
    private const string SkipReason = "Set COMMERZBANK_SANDBOX_CLIENT_ID and COMMERZBANK_SANDBOX_CLIENT_SECRET to run sandbox tests.";

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task HeartbeatAsync_Sandbox_Succeeds()
    {
        await fixture.Client.HeartbeatAsync();
        output.WriteLine("Heartbeat succeeded.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task GetAccessTokenAsync_Sandbox_ReturnsTokenWithFutureExpiry()
    {
        var tokenProvider = (ClientCredentialsTokenProvider)fixture.Services.GetRequiredService<IAccessTokenProvider>();
        await tokenProvider.GetAccessTokenAsync();

        var token = tokenProvider.CurrentToken;
        token.ShouldNotBeNull();
        token!.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        output.WriteLine($"Access token expires in {(token.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds:F0}s, has refresh token: {token.HasRefreshToken}.");
    }

    [Fact(SkipUnless = nameof(SandboxCredentials.Available), SkipType = typeof(SandboxCredentials), Skip = SkipReason)]
    public async Task GetAccessTokenAsync_Sandbox_ReturnsCachedTokenWithinValidity()
    {
        var tokenProvider = (ClientCredentialsTokenProvider)fixture.Services.GetRequiredService<IAccessTokenProvider>();

        var first = await tokenProvider.GetAccessTokenAsync();
        var second = await tokenProvider.GetAccessTokenAsync();

        second.ShouldBe(first);
        output.WriteLine("Second call within validity returned the cached token.");
    }
}
