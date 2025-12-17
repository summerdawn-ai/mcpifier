using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IHost"/> to use Mcpify services.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Activates the Mcpify server to handle stdio traffic.
    /// </summary>
    /// <param name="app">The <see cref="IHost"/> which has been configured to host Mcpify.</param>
    /// <exception cref="InvalidOperationException">The Mcpify configuration is invalid or incomplete.</exception>
    /// <returns>The same <see cref="IHost"/> instance provided in <paramref name="app"/>.</returns>
    public static IHost UseMcpify(this IHost app)
    {
        var services = app.Services;

        var server = app.Services.GetService<McpStdioServer>() ??
                     throw new InvalidOperationException("Unable to find required services. You must call builder.Services.AddMcpify() in application startup code.");

        var options = services.GetRequiredService<IOptions<McpifyOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<McpifyBuilder>>();

        // Log the mode and base address, but do not verify or throw -
        // for all we know, the user may have injected a different HttpClient.
        logger.LogInformation("Mcpify is configured to listen to MCP traffic on STDIO and forward tool calls to '{restBaseAddress}'.", options.Rest.BaseAddress);

        // Verify any tools are configured.
        services.ThrowIfNoMcpifyTools();
        services.LogMcpifyTools();

        // Warn if we're using settings that are not supported over STDIO.
        services.WarnIfUnsupportedMcpifyStdioOptions();

        // Activate the registered background service.
        server.Activate();

        return app;
    }
}
