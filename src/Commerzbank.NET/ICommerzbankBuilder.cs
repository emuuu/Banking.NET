using Microsoft.Extensions.DependencyInjection;

namespace Commerzbank.NET;

/// <summary>Builder returned by <see cref="CommerzbankServiceCollectionExtensions"/> used to register individual Commerzbank APIs (e.g. Corporate Payments).</summary>
public interface ICommerzbankBuilder
{
    /// <summary>The service collection the Commerzbank services were registered into.</summary>
    IServiceCollection Services { get; }
}

internal sealed class CommerzbankBuilder(IServiceCollection services) : ICommerzbankBuilder
{
    public IServiceCollection Services { get; } = services;
}
