using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IHost"/> to use Mcpifier services.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Activates the Mcpifier server to handle stdio traffic.
    /// </summary>
    /// <param name="app">The <see cref="IHost"/> which has been configured to host Mcpifier.</param>
    /// <exception cref="InvalidOperationException">The Mcpifier configuration is invalid or incomplete.</exception>
    /// <returns>The same <see cref="IHost"/> instance provided in <paramref name="app"/>.</returns>
    public static IHost UseMcpifier(this IHost app)
    {
        var services = app.Services;

        var server = app.Services.GetService<McpStdioServer>() ??
                     throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpifier() in application startup code.");

        var options = services.GetRequiredService<IOptions<McpifierOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<McpifierBuilder>>();

        // Log the mode and base address, but do not verify or throw -
        // for all we know, the user may have injected a different HttpClient.
        logger.LogInformation("Mcpifier is configured to listen to MCP traffic on STDIO and forward tool calls to '{restBaseAddress}'.", options.Rest.BaseAddress);

        // Verify any tools are configured.
        services.ThrowIfNoMcpifierTools();
        services.LogMcpifierTools();

        // Warn if we're using settings that are not supported over STDIO.
        services.WarnIfUnsupportedMcpifierStdioOptions();

        // Activate the registered background service.
        server.Activate();

        return app;
    }
}
