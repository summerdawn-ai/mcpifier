using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mcpify.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses ProgramHttp's CreateHostBuilder method.
/// </summary>
public class McpifyServerFactory : WebApplicationFactory<Summerdawn.Mcpify.Server.ProgramHttp>
{
    protected override IHostBuilder? CreateHostBuilder()
    {
        return Summerdawn.Mcpify.Server.ProgramHttp.CreateHostBuilder(Array.Empty<string>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to test directory so it uses the test mappings.json
        builder.UseContentRoot(AppContext.BaseDirectory);
        
        base.ConfigureWebHost(builder);
    }
}
