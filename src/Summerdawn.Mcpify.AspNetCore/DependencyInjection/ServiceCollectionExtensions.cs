using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpify(this IServiceCollection services, Action<McpifyOptions> configureOptions)
    {
        // Configure options from action.
        services.Configure(configureOptions);

        return services.AddMcpify();
    }

    public static IServiceCollection AddMcpify(this IServiceCollection services, IConfiguration mcpifyConfiguration)
    {
        // Configure options from provided config section.
        services.Configure<McpifyOptions>(mcpifyConfiguration);

        return services.AddMcpify();
    }

    private static IServiceCollection AddMcpify(this IServiceCollection services)
    {
        // Add core services.
        CoreServiceCollectionExtensions.AddMcpifyCore(services);

        // Add overriding base address creator with support for
        // base addresses relative to the server's address.
        services.AddKeyedSingleton<Uri>("Mcpify:Rest:BaseAddress", (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptions<McpifyOptions>>();

            var baseAddress = new Uri(options.Value.Rest.BaseAddress, UriKind.RelativeOrAbsolute);

            // Return configured base address if absolute.
            if (baseAddress.IsAbsoluteUri) return baseAddress;

            // Otherwise, combine with own server address.
            var serverAddress = GetServerAddress(provider);

            return new Uri(serverAddress, baseAddress);
        });

        // Add context accessor for forwarding HTTP headers.
        services.AddHttpContextAccessor();

        // Add ASP.NET Core HTTP routing handler.
        services.AddSingleton<McpRouteHandler>();

        return services;
    }

    private static Uri GetServerAddress(IServiceProvider provider)
    {
        var serverFeatures = provider.GetService<IServer>();
        var addressesFeature = serverFeatures?.Features.Get<IServerAddressesFeature>();
        var serverAddress = addressesFeature?.Addresses.FirstOrDefault() ??
                            throw new InvalidOperationException("REST base address is relative, but no server address is available. Either configure an absolute " +
                                                                "base address, or ensure than an ASP.NET Core IServer with at least one address is registered.");

        return NormalizeHost(new Uri(serverAddress));
    }

    private static Uri NormalizeHost(Uri uri)
    {
        if (uri.Host is "0.0.0.0" or "::")
        {
            var builder = new UriBuilder(uri) { Host = "localhost" };
            return builder.Uri;
        }

        return uri;
    }
}