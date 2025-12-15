using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IHost"/> to use Mcpify services.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Activates the Mcpify server for the specified host application.
    /// </summary>
    /// <remarks>This method enables the Mcpify server by retrieving and activating the required <see
    /// cref="McpStdioServer"/> service from the application's service provider. Call this method during application
    /// startup to ensure the server is active.</remarks>
    /// <param name="app">The host application to configure with the Mcpify server. Cannot be null.</param>
    /// <returns>The same <see cref="IHost"/> instance provided in <paramref name="app"/>.</returns>
    public static IHost UseMcpify(this IHost app)
    {
        var services = app.Services;

        var server = app.Services.GetService<McpStdioServer>() ??
                     throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpify() in application startup code.");

        var options = services.GetRequiredService<IOptions<McpifyOptions>>().Value;

        // Log Mcpify information
        var logger = services.GetRequiredService<ILogger<RestProxyService>>();

        logger.LogInformation("Mcpify is configured to handle STDIO MCP traffic.");

        string toolsList = string.Join("\r\n", options.Tools.Select(tool => $"  - {tool.Mcp.Name}: {tool.Mcp.Description}"));
        logger.LogInformation("Successfully loaded {toolCount} tools:\r\n{toolList}", options.Tools.Count, toolsList);

        // Warn if we're using settings that are not supported over STDIO.
        var forwardedHeaderNames = options.Rest.ForwardedHeaders.Where(h => h.Value == true).Select(h => h.Key).ToList();
        if (forwardedHeaderNames.Any())
        {
            logger.LogWarning("Header forwarding is configured for {count} headers, but MCP over STDIO does not support header forwarding. You should disable header forwarding or use MCP over HTTP.", forwardedHeaderNames.Count);
        }

        if (options.Authentication.RequireAuthorization)
        {
            logger.LogWarning("Authorization is configured as required, but MCP over STDIO does not support authorization. You should disable authorization or use MCP over HTTP.");
        }

        // Activate the registered background service.
        server.Activate();

        return app;
    }
}
