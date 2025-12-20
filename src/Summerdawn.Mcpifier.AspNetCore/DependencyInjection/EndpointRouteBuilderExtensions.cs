using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure Mcpifier.
/// application.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps MCP (Model Context Protocol) HTTP(S) endpoints to Mcpifier's gateway.
    /// </summary>
    /// <remarks>
    /// Ensure that you have called <c>builder.Services.AddMcpifier().AddAspNetCore()</c> in application startup code.
    /// All mapped endpoints can be further customized using the returned convention builder.
    /// </remarks>
    public static IEndpointConventionBuilder MapMcpifier(this IEndpointRouteBuilder endpoints, string route = "")
    {
        var services = endpoints.ServiceProvider;

        var handler = services.GetService<McpRouteHandler>() ??
                              throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpifier().AddAspNetCore() in application startup code.");

        var options = services.GetRequiredService<IOptions<McpifierOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<McpifierBuilder>>();

        // Log the mode and base address, but do not verify or throw -
        // for all we know, the user may have injected a different HttpClient.
        logger.LogInformation("Mcpifier is configured to listen to MCP traffic on HTTP and forward tool calls to '{restBaseAddress}'.", options.Rest.BaseAddress);

        services.ThrowIfNoMcpifierTools();
        services.LogMcpifierTools();

        // Set up protected resource metadata endpoint if configured.
        // This endpoint will _not_ be affected by configuration of the main route (e.g. RequireAuthorization).
        if (options.Authorization.ResourceMetadata is not null)
        {
            endpoints.MapGet($"/.well-known/oauth-protected-resource/{route}", handler.HandleProtectedResourceAsync);
        }

        // Set up mapping, and return builder to allow configuring route further.
        return endpoints.MapPost(route, handler.HandleMcpRequestAsync);
    }
}