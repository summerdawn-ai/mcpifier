using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/> to verify and log Mcpifier tools.
/// </summary>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Logs the given server mode and configured REST API base address, or logs
    /// an error and throws an exception if no suitable base address is configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">No suitable REST API base address is configured.</exception>
    public static IServiceProvider LogBaseAddressOrThrow(this IServiceProvider serviceProvider, string mode)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifierOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<McpifierBuilder>>();

        string restBaseAddress = options.Rest.BaseAddress;

        if (string.IsNullOrWhiteSpace(restBaseAddress))
        {
            logger.LogError("No REST API base address has been configured.");
            
            throw new InvalidOperationException("No REST API base address has been configured.");
        }

        if (mode == "stdio" && !Uri.TryCreate(restBaseAddress, UriKind.Absolute, out _))
        {
            logger.LogError("The configured REST API base address is '{restBaseAddress}', but MCP over stdio only supports absolute base addresses.", restBaseAddress);

            throw new InvalidOperationException($"The configured REST API base address is '{restBaseAddress}', but MCP over stdio only supports absolute base addresses.");
        }

        logger.LogInformation("Mcpifier is configured to listen to MCP traffic on {mode} and forward tool calls to '{restBaseAddress}'.", mode, restBaseAddress);

        return serviceProvider;
    }

    /// <summary>
    /// Logs the list of configured Mcpifier tools, or logs an error
    /// and throws an exception if no tools are configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">No tools mappings have been found in the configuration.</exception>
    public static IServiceProvider LogMcpifierToolsOrThrow(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifierOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<McpifierBuilder>>();

        // Throw error (and show instructions) if tools list is empty.
        if (options.Tools.Count == 0)
        {
            string contentRoot = serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            logger.LogError("No tool mappings have been configured. The default location for mappings files is '{contentRoot}'.", contentRoot);

            throw new InvalidOperationException("No tool mappings have been configured.");
        }

        string toolsList = string.Join('\n', options.Tools.Select(tool => $"  - {tool.Mcp.Name}: {tool.Mcp.Description}"));
        logger.LogInformation("Successfully loaded {toolCount} tools:\n{toolList}", options.Tools.Count, toolsList);

        return serviceProvider;
    }

    /// <summary>
    /// Verifies that no Mcpifier options are configured that are not supported when using MCP over stdio, otherwise logs a warning.
    /// </summary>
    public static IServiceProvider WarnIfUnsupportedMcpifierStdioOptions(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<McpifierOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<McpifierBuilder>>();

        var forwardedHeaderNames = options.Rest.ForwardedHeaders.Where(h => h.Value == true).Select(h => h.Key).ToList();
        if (forwardedHeaderNames.Any())
        {
            logger.LogWarning("Header forwarding is configured for {count} headers, but MCP over stdio does not support header forwarding. You should disable header forwarding or use MCP over HTTP.", forwardedHeaderNames.Count);
        }

        if (options.Authorization.RequireAuthorization)
        {
            logger.LogWarning("Authorization is configured as required, but MCP over stdio does not support authorization. You should disable authorization or use MCP over HTTP.");
        }

        return serviceProvider;
    }
}