using System.CommandLine;

using Summerdawn.Mcpify.DependencyInjection;

namespace Summerdawn.Mcpify.Server;

/// <summary>
/// Main program class for the Mcpify server.
/// </summary>
public  class Program
{
    /// <summary>
    /// Entry point for the Mcpify server application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        var modeOption = new Option<string>("--mode", "The server mode to use")
        {
            IsRequired = true
        };
        modeOption.AddAlias("-m");
        modeOption.FromAmong("http", "stdio");

        var rootCommand = new RootCommand("MCP server that can run in HTTP or stdio mode");
        rootCommand.AddOption(modeOption);
        rootCommand.SetHandler(mode => MainWithMode(args, mode), modeOption);

        return rootCommand.Invoke(args);
    }

    private static void MainWithMode(string[] args, string mode)
    {
        if (mode == "http")
        {
            // Delegate to HTTP-only entry point for WebApplicationFactory compatibility.
            var app = ProgramHttp.CreateHostBuilder(args).Build();
            app.Run();
        }
        else
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load tool mappings from separate file.
            // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
            try
            {
                builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: true);
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

            // Configure stdio MCP proxy.
            try
            {
                builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Configuration error: Failed to configure Mcpify services. {ex.Message}");
                throw new InvalidOperationException("Failed to configure Mcpify services. Check the Mcpify configuration section in appsettings.json and mappings.json.", ex);
            }

            // Send all console logging output to stderr so that it doesn't interfere with MCP stdio traffic.
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            var app = builder.Build();

            // Use stdio MCP proxy.
            app.UseMcpify();

            app.Run();
        }
    }
}