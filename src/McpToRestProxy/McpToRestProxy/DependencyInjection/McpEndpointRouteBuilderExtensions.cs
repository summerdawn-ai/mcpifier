using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.McpToRestProxy.Configuration;
using Summerdawn.McpToRestProxy.Services;

namespace Summerdawn.McpToRestProxy.DependencyInjection;

/// <summary>
/// Provides extension methods for mapping MCP (Model-Centric Proxy) endpoints to REST proxy routes in an ASP.NET Core
/// application.
/// </summary>
/// <remarks>These extensions enable integration of MCP tools with RESTful endpoints, allowing requests to be
/// proxied through configured handlers. To use these methods, ensure that the required services are registered by
/// calling AddMcpToRestProxy() during application startup. The extensions also configure a protected resource metadata
/// endpoint if authentication options are set. All mapped endpoints can be further customized using the returned
/// convention builder.</remarks>
public static class McpEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps Model Context Protocol requests to REST proxy endpoints.
    /// </summary>
    public static IEndpointConventionBuilder MapMcpToRestProxy(this IEndpointRouteBuilder endpoints, string route = "")
    {
        var services = endpoints.ServiceProvider;

        var handler = services.GetService<McpRouteHandler>() ??
                      throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpToRestProxy() in application startup code.");

        var proxyOptions = services.GetRequiredService<IOptions<ProxyOptions>>().Value;

        // Log tool information
        var logger = services.GetRequiredService<ILogger<RestProxyService>>();

        logger.LogInformation("Successfully loaded {toolCount} tools", proxyOptions.Tools.Count);
        foreach (var tool in proxyOptions.Tools)
        {
            logger.LogInformation("  - {toolName}: {description}", tool.Mcp.Name, tool.Mcp.Description);
        }

        // Set up protected resource metadata endpoint if configured.
        // This endpoint will _not_ be affected by configuration of the main route (e.g. RequireAuthorization).
        if (proxyOptions.Authentication.ResourceMetadata is not null)
        {
            endpoints.MapGet($"/.well-known/oauth-protected-resource/{route}", handler.HandleProtectedResourceAsync);
        }

        // Set up mapping, and return builder to allow configuring route further.
        return endpoints.MapPost(route, handler.HandleMcpRequestAsync);
    }
}