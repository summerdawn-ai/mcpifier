using Summerdawn.Mcpify.DependencyInjection;

namespace Summerdawn.Mcpify.Server;

/// <summary>
/// HTTP-only entry point for use with WebApplicationFactory in tests.
/// </summary>
public class ProgramHttp
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((context, services) =>
                {
                    // Load tool mappings from separate file.
                    // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                    
                    // Configure HTTP MCP proxy.
                    services.AddMcpify(context.Configuration.GetSection("Mcpify")).AddAspNetCore();

                    // Configure CORS to allow any connection.
                    services.AddCors(cors => cors.AddDefaultPolicy(policy =>
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
                });

                webBuilder.Configure((context, app) =>
                {
                    app.UseHttpsRedirection();
                    app.UseRouting();
                    app.UseCors();
                    
                    app.UseEndpoints(endpoints =>
                    {
                        // Use HTTP MCP proxy.
                        endpoints.MapMcpify();
                    });
                });
                
                webBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("mappings.json", optional: false, reloadOnChange: true);
                });
            });
}
