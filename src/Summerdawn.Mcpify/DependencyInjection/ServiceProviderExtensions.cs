using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpify.Configuration;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/> to verify and log Mcpify tools.
/// </summary>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Verifies that Mcpify tools have been configured, otherwise logs an error and throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">No tools mappings have been found in the configuration.</exception>
    public static IServiceProvider ThrowIfNoMcpifyTools(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifyOptions>>().Value;
        // Throw error (and show instructions) if tools list is empty.
        if (options.Tools.Count == 0)
        {
            string contentRoot = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            var logger = serviceProvider.GetRequiredService<ILogger<McpifyBuilder>>();

            logger.LogError("No tool mappings were found in the app configuration. The expected location for configuration files is '{contentRoot}'.", contentRoot);
            
            throw new InvalidOperationException($"No tool mappings were found in the app configuration.");
        }

        return serviceProvider;
    }

    /// <summary>
    /// Logs the list of configured Mcpify tools.
    /// </summary>
    public static IServiceProvider LogMcpifyTools(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifyOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<McpifyBuilder>>();

        string toolsList = string.Join("\r\n", options.Tools.Select(tool => $"  - {tool.Mcp.Name}: {tool.Mcp.Description}"));
        logger.LogInformation("Successfully loaded {toolCount} tools:\r\n{toolList}", options.Tools.Count, toolsList);

        return serviceProvider;
    }

    /// <summary>
    /// Verifies that no Mcpify options are configured that are not supported when using MCP over STDIO, otherwise logs a warning.
    /// </summary>
    public static IServiceProvider WarnIfUnsupportedMcpifyStdioOptions(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifyOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<McpifyBuilder>>();

        var forwardedHeaderNames = options.Rest.ForwardedHeaders.Where(h => h.Value == true).Select(h => h.Key).ToList();
        if (forwardedHeaderNames.Any())
        {
            logger.LogWarning("Header forwarding is configured for {count} headers, but MCP over STDIO does not support header forwarding. You should disable header forwarding or use MCP over HTTP.", forwardedHeaderNames.Count);
        }

        if (options.Authentication.RequireAuthorization)
        {
            logger.LogWarning("Authorization is configured as required, but MCP over STDIO does not support authorization. You should disable authorization or use MCP over HTTP.");
        }

        return serviceProvider;
    }
}