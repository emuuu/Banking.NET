using System.Net.Http;
using Banking.NET.Commerzbank.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Banking.NET.Commerzbank;

/// <summary>Extension methods for registering Commerzbank API infrastructure with an <see cref="IServiceCollection"/>.</summary>
public static class CommerzbankServiceCollectionExtensions
{
    /// <summary>Registers the Commerzbank infrastructure (options, authentication, named HTTP clients) configured by a delegate.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate that configures <see cref="CommerzbankOptions"/>.</param>
    /// <returns>A builder used to register individual Commerzbank APIs.</returns>
    public static ICommerzbankBuilder AddCommerzbank(this IServiceCollection services, Action<CommerzbankOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return AddCommerzbankCore(services);
    }

    /// <summary>Registers the Commerzbank infrastructure (options, authentication, named HTTP clients) bound from the given configuration section.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind <see cref="CommerzbankOptions"/> from (e.g. `configuration.GetSection(CommerzbankOptions.SectionName)`).</param>
    /// <returns>A builder used to register individual Commerzbank APIs.</returns>
    public static ICommerzbankBuilder AddCommerzbank(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CommerzbankOptions>(configuration);
        return AddCommerzbankCore(services);
    }

    private static ICommerzbankBuilder AddCommerzbankCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<CommerzbankOptions>, CommerzbankOptionsValidator>());
        services.AddOptions<CommerzbankOptions>().ValidateOnStart();

        services.TryAddSingleton<ClientCertificateProvider>();
        services.TryAddSingleton<IAccessTokenProvider>(CreateAccessTokenProvider);
        services.TryAddTransient<CommerzbankAuthHandler>();

        services.AddHttpClient(CommerzbankHttpClientNames.Token, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CommerzbankOptions>>().Value;
            client.Timeout = options.TokenTimeout;
        }).ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        services.AddHttpClient(CommerzbankHttpClientNames.Api, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CommerzbankOptions>>().Value;
            client.BaseAddress = options.GetApiBaseUri();
            client.Timeout = options.Timeout;
            if (!string.IsNullOrEmpty(options.ClientProduct))
            {
                client.DefaultRequestHeaders.Remove("ClientProduct");
                client.DefaultRequestHeaders.TryAddWithoutValidation("ClientProduct", options.ClientProduct);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler)
        .AddHttpMessageHandler<CommerzbankAuthHandler>();

        return new CommerzbankBuilder(services);
    }

    private static IAccessTokenProvider CreateAccessTokenProvider(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<CommerzbankOptions>>();
        if (options.Value.AccessTokenProvider is { } accessTokenProvider)
            return new DelegateAccessTokenProvider(accessTokenProvider);

        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        return new ClientCredentialsTokenProvider(httpClientFactory, options);
    }

    internal static HttpMessageHandler CreatePrimaryHandler(IServiceProvider serviceProvider)
    {
        var handler = new HttpClientHandler();
        var certificate = serviceProvider.GetRequiredService<ClientCertificateProvider>().Certificate;
        if (certificate is not null)
        {
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ClientCertificates.Add(certificate);
        }

        return handler;
    }

    private sealed class CommerzbankOptionsValidator : IValidateOptions<CommerzbankOptions>
    {
        public ValidateOptionsResult Validate(string? name, CommerzbankOptions options)
        {
            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (InvalidOperationException ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }
}
