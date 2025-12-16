using Summerdawn.Mcpify.DependencyInjection;

namespace Summerdawn.Mcpify.Server;

/// <summary>
/// HTTP-only entry point for use with WebApplicationFactory in tests.
/// This class provides a CreateHostBuilder method that WebApplicationFactory can discover.
/// </summary>
public class ProgramHttp
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    // Load tool mappings from separate file.
                    // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                    try
                    {
                        config.AddJsonFile("mappings.json", optional: false, reloadOnChange: true);
                    }
                    catch (FileNotFoundException ex)
                    {
                        Console.Error.WriteLine($"Configuration error: mappings.json file not found. {ex.Message}");
                        throw new InvalidOperationException("Failed to load required configuration file 'mappings.json'. Ensure the file exists in the content root directory.", ex);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Configuration error: Failed to load mappings.json. {ex.Message}");
                        throw new InvalidOperationException("Failed to load configuration file 'mappings.json'. Check the file format and permissions.", ex);
                    }
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    // Configure HTTP MCP proxy.
                    try
                    {
                        services.AddMcpify(context.Configuration.GetSection("Mcpify")).AddAspNetCore();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Configuration error: Failed to configure Mcpify services. {ex.Message}");
                        throw new InvalidOperationException("Failed to configure Mcpify services. Check the Mcpify configuration section in appsettings.json and mappings.json.", ex);
                    }

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
            });
}
