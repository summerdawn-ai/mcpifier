using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses Mcpifier.Server's main entry point.
/// </summary>
public class McpifierServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to test directory so it uses the test mappings.json
        builder.UseContentRoot(AppContext.BaseDirectory);

        base.ConfigureWebHost(builder);
    }
}
