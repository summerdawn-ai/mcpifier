using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Abstractions;
using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Handlers;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to configure Mcpifier services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Mcpifier services to the service collection with the specified configuration action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure Mcpifier options.</param>
    /// <returns>A <see cref="McpifierBuilder"/> that can be used to further configure Mcpifier.</returns>
    public static McpifierBuilder AddMcpifier(this IServiceCollection services, Action<McpifierOptions> configureOptions)
    {
        // Configure options from action.
        services.Configure(configureOptions);

        return services.AddMcpifierCore();
    }

    /// <summary>
    /// Adds Mcpifier services to the service collection with configuration from the specified configuration section.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="mcpifierConfiguration">The configuration section containing Mcpifier settings.</param>
    /// <returns>A <see cref="McpifierBuilder"/> that can be used to further configure Mcpifier.</returns>
    public static McpifierBuilder AddMcpifier(this IServiceCollection services, IConfiguration mcpifierConfiguration)
    {
        // Configure options from provided config section.
        services.Configure<McpifierOptions>(mcpifierConfiguration);

        return services.AddMcpifierCore();
    }

    /// <summary>
    /// Adds REST API service, JSON-RPC handlers, and stdio server.
    /// </summary>
    private static McpifierBuilder AddMcpifierCore(this IServiceCollection services)
    {
        // Add stdio abstraction.
        services.AddSingleton<IStdio, ConsoleStdio>();

        // Add REST API http client.
        services.AddHttpClient<RestApiService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<McpifierOptions>>();
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

        // Add stdio MCP server.
        services.AddSingleton<McpStdioServer>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<McpStdioServer>());

        return new McpifierBuilder(services);
    }

    private static Uri GetBaseAddress(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<McpifierOptions>>();

        Uri baseAddress;
        try
        {
            baseAddress = new Uri(options.Value.Rest.BaseAddress, UriKind.RelativeOrAbsolute);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid base address '{options.Value.Rest.BaseAddress}' in Mcpifier configuration. Ensure it is a valid URI.", ex);
        }

        // Return configured base address if absolute.
        if (baseAddress.IsAbsoluteUri) return baseAddress;

        // Otherwise, get server address from an extension, e.g. Mcpifier.AspNetCore.
        var serverAddress = provider.GetKeyedService<Uri>("Mcpifier:ServerAddress") ??
                            throw new InvalidOperationException("REST base address is relative, but no server address is available. Either configure an absolute " +
                                                                "base address, or ensure than the Mcpifier service builder registers a server address.");

        // Normalize "0.0.0.0" host to "localhost".
        serverAddress = NormalizeHost(serverAddress);

        // And build absolute URI based on server address.
        return new Uri(serverAddress, baseAddress);
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
