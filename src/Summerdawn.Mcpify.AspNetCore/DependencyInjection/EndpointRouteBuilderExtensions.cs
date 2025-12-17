using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure Mcpify.
/// application.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps MCP (Model Context Protocol) HTTP(S) endpoints to Mcpify's REST proxy.
    /// </summary>
    /// <remarks>
    /// Ensure that you have called <c>builder.Services.AddMcpify().AddAspNetCore()</c> in application startup code.
    /// All mapped endpoints can be further customized using the returned convention builder.
    /// </remarks>
    public static IEndpointConventionBuilder MapMcpify(this IEndpointRouteBuilder endpoints, string route = "")
    {
        var services = endpoints.ServiceProvider;

        var handler = services.GetService<McpRouteHandler>() ??
                              throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpify().AddAspNetCore() in application startup code.");

        var options = services.GetRequiredService<IOptions<McpifyOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<McpifyBuilder>>();

        // Log the mode and base address, but do not verify or throw -
        // for all we know, the user may have injected a different HttpClient.
        logger.LogInformation("Mcpify is configured to listen to MCP traffic on HTTP and forward tool calls to '{restBaseAddress}'.", options.Rest.BaseAddress);

        services.ThrowIfNoMcpifyTools();
        services.LogMcpifyTools();

        // Set up protected resource metadata endpoint if configured.
        // This endpoint will _not_ be affected by configuration of the main route (e.g. RequireAuthorization).
        if (options.Authentication.ResourceMetadata is not null)
        {
            endpoints.MapGet($"/.well-known/oauth-protected-resource/{route}", handler.HandleProtectedResourceAsync);
        }

        // Set up mapping, and return builder to allow configuring route further.
        return endpoints.MapPost(route, handler.HandleMcpRequestAsync);
    }
}