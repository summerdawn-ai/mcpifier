using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Server.Tests;

public class McpifierHostFactory
{
    public IHost WithApplicationBuilder(Action<HostApplicationBuilder> builderAction)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: false);
        builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

        // Set server info for testing
        builder.Services.Configure<McpifierOptions>(mcpifier =>
            mcpifier.ServerInfo = new McpServerInfo { Name = "mcpifier", Version = "1.0" });

        builderAction.Invoke(builder);

        return builder.Build();
    }
}
