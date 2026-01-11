using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses Mcpifier.Server's main entry point.
/// </summary>
public class McpifierServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);

        // Re-add Mcpifier to get builder and load mappings
        builder.ConfigureServices((context, services) => services.AddMcpifier(context.Configuration.GetSection("Mcpifier")).AddAspNetCore()
                .AddToolsFromMappings("Resources/test-mappings.json"));

        // Set server info for testing
        builder.ConfigureServices(services => services.Configure<McpifierOptions>(mcpifier =>
            mcpifier.ServerInfo = new McpServerInfo { Name = "mcpifier", Version = "1.0" }));

        base.ConfigureWebHost(builder);
    }
}
