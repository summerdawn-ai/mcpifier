using System.CommandLine;

using Summerdawn.Mcpify.DependencyInjection;

namespace Summerdawn.Mcpify.Server;

/// <summary>
/// Main program class for the Mcpify server.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the Mcpify server application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        // Support testing via WebApplicationFactory.
        // This approach allows us to reuse the modern WebApplicationBuilder instead
        // of having to work with a custom entry point and legacy IHostBuilder.
        if (args.Any(a => a.Contains("Summerdawn.Mcpify.Server.Tests")))
        {
            // Remove the args specified by the test framework, and select HTTP mode.
            args = ["--mode=http"];
        }

        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "The server mode to use.",
            Required = true
        }.AcceptOnlyFromAmong("http", "stdio");

        var rootCommand = new RootCommand("MCP server that can run in HTTP or stdio mode")
        {
            modeOption
        };
        rootCommand.SetAction(parseResult =>
        {
            string mode = parseResult.GetValue(modeOption)!;

            MainWithMode(args, mode);
        });

        try
        {
            return rootCommand.Parse(args).Invoke(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void MainWithMode(string[] args, string mode)
    {
        if (mode == "http")
        {
            var builder = WebApplication.CreateBuilder();

            // Load tool mappings from separate file.
            // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
            builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: true);

            // Configure HTTP MCP proxy.
            builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify")).AddAspNetCore();

            // Configure CORS to allow any connection.
            builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();

            // Use HTTP MCP proxy.
            app.MapMcpify();

            app.Run();
        }
        else
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load tool mappings from separate file.
            // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
            builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: true);

            // Configure stdio MCP proxy.
            builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"));

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