using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Handlers;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

public static class ServiceCollectionHelper
{
    /// <summary>
    /// Adds core Mcpify services and JSON-RPC handlers, but no handler for HTTP routing.
    /// </summary>
    public static IServiceCollection AddMcpifyCore(IServiceCollection services)
    {
        // Add REST API http client.
        services.AddHttpClient<RestProxyService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<McpifyOptions>>();

            client.BaseAddress = new Uri(options.Value.ApiBaseUrl);

            foreach (var defaultHeader in options.Value.ApiDefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(defaultHeader.Key, defaultHeader.Value);
            }
        });

        // Add JSON-RPC dispatcher, handlers and factory.
        services.AddSingleton<JsonRpcDispatcher>();

        services.AddKeyedSingleton<IRpcHandler, McpPingRpcHandler>("ping");
        services.AddKeyedSingleton<IRpcHandler, McpInitializeRpcHandler>("initialize");
        services.AddKeyedSingleton<IRpcHandler, McpNotificationsInitializedRpcHandler>("notifications/initialized");
        services.AddKeyedSingleton<IRpcHandler, McpToolsListRpcHandler>("tools/list");
        services.AddKeyedTransient<IRpcHandler, McpToolsCallRpcHandler>("tools/call");

        services.AddTransient<Func<string, IRpcHandler?>>(serviceProvider => serviceProvider.GetKeyedService<IRpcHandler>);

        return services;
    }
}