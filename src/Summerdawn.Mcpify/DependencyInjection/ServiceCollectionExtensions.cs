using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpify.Abstractions;
using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Handlers;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to configure Mcpify services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Mcpify services to the service collection with the specified configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure Mcpify options.</param>
    /// <returns>A <see cref="McpifyBuilder"/> that can be used to further configure Mcpify.</returns>
    public static McpifyBuilder AddMcpify(this IServiceCollection services, Action<McpifyOptions> configureOptions)
    {
        // Configure options from action.
        services.Configure(configureOptions);

        return services.AddMcpifyCore();
    }

    /// <summary>
    /// Adds Mcpify services to the service collection with configuration from the specified configuration section.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="mcpifyConfiguration">The configuration section containing Mcpify settings.</param>
    /// <returns>A <see cref="McpifyBuilder"/> that can be used to further configure Mcpify.</returns>
    public static McpifyBuilder AddMcpify(this IServiceCollection services, IConfiguration mcpifyConfiguration)
    {
        // Configure options from provided config section.
        try
        {
            services.Configure<McpifyOptions>(mcpifyConfiguration);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to bind Mcpify configuration. Check the configuration section format and values.", ex);
        }

        return services.AddMcpifyCore();
    }

    /// <summary>
    /// Adds Mcpify proxy, JSON-RPC handlers, and stdio server.
    /// </summary>
    private static McpifyBuilder AddMcpifyCore(this IServiceCollection services)
    {
        // Add stdio abstraction.
        services.AddSingleton<IStdio, ConsoleStdio>();

        // Add REST API http client.
        services.AddHttpClient<RestProxyService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<McpifyOptions>>();
            var baseAddress = GetBaseAddress(provider);

            client.BaseAddress = baseAddress;

            foreach (var defaultHeader in options.Value.Rest.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(defaultHeader.Key, defaultHeader.Value);
            }
        });

        // Add JSON-RPC dispatcher, handlers and factory.
        services.AddSingleton<IJsonRpcDispatcher, JsonRpcDispatcher>();
        services.AddSingleton<JsonRpcDispatcher>();

        services.AddKeyedSingleton<IRpcHandler, McpPingRpcHandler>("ping");
        services.AddKeyedSingleton<IRpcHandler, McpInitializeRpcHandler>("initialize");
        services.AddKeyedSingleton<IRpcHandler, McpNotificationsInitializedRpcHandler>("notifications/initialized");
        services.AddKeyedSingleton<IRpcHandler, McpToolsListRpcHandler>("tools/list");
        services.AddKeyedTransient<IRpcHandler, McpToolsCallRpcHandler>("tools/call");

        services.AddTransient<Func<string, IRpcHandler?>>(serviceProvider => serviceProvider.GetKeyedService<IRpcHandler>);

        // Add stdio MCP proxy.
        services.AddSingleton<McpStdioServer>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<McpStdioServer>());

        return new McpifyBuilder(services);
    }

    private static Uri GetBaseAddress(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<McpifyOptions>>();

        Uri baseAddress;
        try
        {
            baseAddress = new Uri(options.Value.Rest.BaseAddress, UriKind.RelativeOrAbsolute);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid base address '{options.Value.Rest.BaseAddress}' in Mcpify configuration. Ensure it is a valid URI.", ex);
        }

        // Return configured base address if absolute.
        if (baseAddress.IsAbsoluteUri) return baseAddress;

        // Otherwise, get server address from an extension, e.g. Mcpify.AspNetCore.
        var serverAddress = provider.GetKeyedService<Uri>("Mcpify:ServerAddress") ??
                            throw new InvalidOperationException("REST base address is relative, but no server address is available. Either configure an absolute " +
                                                                "base address, or ensure than the Mcpify service builder registers a server address.");

        // Normalize "0.0.0.0" host to "localhost".
        serverAddress = NormalizeHost(serverAddress);

        // And build absolute URI based on server address.
        try
        {
            return new Uri(serverAddress, baseAddress);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to build absolute URI from server address '{serverAddress}' and base address '{baseAddress}'.", ex);
        }
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
