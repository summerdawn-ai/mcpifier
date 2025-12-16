using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Summerdawn.Mcpify.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses ProgramHttp's CreateHostBuilder method.
/// </summary>
public class McpifyServerFactory : WebApplicationFactory<ProgramHttp>
{
    protected override IHostBuilder? CreateHostBuilder()
    {
        return ProgramHttp.CreateHostBuilder([]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to test directory so it uses the test mappings.json
        builder.UseContentRoot(AppContext.BaseDirectory);
        
        base.ConfigureWebHost(builder);
    }
}
