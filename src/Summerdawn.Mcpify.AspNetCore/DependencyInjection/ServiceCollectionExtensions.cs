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
        ServiceCollectionHelper.AddMcpifyCore(services);

        // Add ASP.NET Core HTTP routing handler.
        services.AddSingleton<McpRouteHandler>();

        // Add context accessor for forwarding HTTP headers.
        services.AddHttpContextAccessor();

        return services;
    }
}