using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.McpToRestProxy.Configuration;
using Summerdawn.McpToRestProxy.Handlers;
using Summerdawn.McpToRestProxy.Services;

namespace Summerdawn.McpToRestProxy.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpToRestProxy(this IServiceCollection services, Action<ProxyOptions> configureOptions)
    {
        // Configure options from action.
        services.Configure(configureOptions);

        return services.AddMcpToRestProxyCore();
    }


    public static IServiceCollection AddMcpToRestProxy(this IServiceCollection services, IConfiguration proxyConfiguration)
    {
        // Configure options from provided config section.
        services.Configure<ProxyOptions>(proxyConfiguration);

        return services.AddMcpToRestProxyCore();
    }

    private static IServiceCollection AddMcpToRestProxyCore(this IServiceCollection services)
    {
        // Add REST API http client.
        services.AddHttpClient<RestProxyService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ProxyOptions>>();

            client.BaseAddress = new Uri(options.Value.ApiBaseUrl);

            foreach (var defaultHeader in options.Value.ApiDefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(defaultHeader.Key, defaultHeader.Value);
            }
        });

        services.AddHttpContextAccessor();

        // Add routing handler.
        services.AddSingleton<McpRouteHandler>();

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