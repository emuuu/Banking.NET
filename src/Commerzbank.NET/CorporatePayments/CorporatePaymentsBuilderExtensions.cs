using Commerzbank.NET.CorporatePayments;
using Microsoft.Extensions.DependencyInjection;

namespace Commerzbank.NET;

/// <summary>Extension methods for registering the Corporate Payments API client with an <see cref="ICommerzbankBuilder"/>.</summary>
public static class CorporatePaymentsBuilderExtensions
{
    /// <summary>Registers <see cref="ICorporatePaymentsClient"/>, backed by the named <see cref="CommerzbankHttpClientNames.Api"/> HTTP client.</summary>
    /// <param name="builder">The Commerzbank builder returned by <see cref="CommerzbankServiceCollectionExtensions.AddCommerzbank(IServiceCollection, Action{CommerzbankOptions})"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ICommerzbankBuilder AddCorporatePayments(this ICommerzbankBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHttpClient<ICorporatePaymentsClient, CorporatePaymentsClient>(CommerzbankHttpClientNames.Api);
        return builder;
    }
}
