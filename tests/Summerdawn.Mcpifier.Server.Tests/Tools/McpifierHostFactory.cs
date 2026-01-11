using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Factory for a host from a HostApplicationBuilder configured for Mcpifier stdio testing.
/// </summary>
public class McpifierHostFactory
{
    public IHost WithApplicationBuilder(Action<HostApplicationBuilder> builderAction)
    {
        var builder = Host.CreateApplicationBuilder();

        // Add Mcpifier and load mappings
        builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
            .AddToolsFromMappings("Resources/test-mappings.json");

        // Set server info for testing
        builder.Services.Configure<McpifierOptions>(mcpifier =>
            mcpifier.ServerInfo = new McpServerInfo { Name = "mcpifier", Version = "1.0" });

        builderAction.Invoke(builder);

        return builder.Build();
    }
}
