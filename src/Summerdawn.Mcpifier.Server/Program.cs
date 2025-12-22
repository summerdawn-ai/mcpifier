using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Main program class for the Mcpifier server.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the Mcpifier server application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        // Support testing via WebApplicationFactory.
        // This approach allows us to reuse the modern WebApplicationBuilder instead
        // of having to work with a custom entry point and legacy IHostBuilder.
        if (args.Any(a => a.Contains("Summerdawn.Mcpifier.Server.Tests")))
        {
            // Remove the args specified by the test framework, and select HTTP mode.
            args = ["--mode=http"];
        }

        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "The server mode to use.",
            Required = true
        }.AcceptOnlyFromAmong("http", "stdio", "mappings");

        var rootCommand = new RootCommand("Mcpifier - an MCP-to-REST gateway that can run in HTTP or stdio mode")
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
        if (mode == "mappings")
        {
            GenerateMappingsFromSwagger();
            return;
        }

        if (mode == "http")
        {
            WebApplication app;
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Load tool mappings from separate file.
                // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                builder.Configuration.AddJsonFile("mappings.json", optional: true, reloadOnChange: false);

                // Configure HTTP MCP gateway.
                builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier")).AddAspNetCore();

                // Configure CORS to allow any connection.
                builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

                app = builder.Build();

                app.UseHttpsRedirection();
                app.UseRouting();
                app.UseCors();

                // Use HTTP MCP gateway.
                app.MapMcpifier();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{ex.Message}\r\n\r\nConsult README.md for instructions on how to configure Mcpifier.", ex);
            }

            app.Run();
        }
        else
        {
            IHost app;
            try
            {
                var builder = Host.CreateApplicationBuilder(args);

                // Load tool mappings from separate file.
                // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                builder.Configuration.AddJsonFile("mappings.json", optional: true, reloadOnChange: false);

                // Configure stdio MCP gateway.
                builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

                // Send all console logging output to stderr so that it doesn't interfere with MCP stdio traffic.
                builder.Logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });

                app = builder.Build();

                // Use stdio MCP gateway.
                app.UseMcpifier();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{ex.Message}\r\n\r\nConsult README.md for instructions on how to configure Mcpifier.", ex);
            }

            app.Run();
        }
    }

    private static void GenerateMappingsFromSwagger()
    {
        string swaggerFilePath = Path.Combine(Directory.GetCurrentDirectory(), "swagger.json");
        string outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "mappings.json");

        // Create a logger factory for the converter
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<SwaggerToMappingConverter>();
        var converter = new SwaggerToMappingConverter(logger);

        try
        {
            Console.WriteLine($"Reading swagger.json from: {swaggerFilePath}");

            string swaggerJson = File.ReadAllText(swaggerFilePath);

            // Convert swagger to tools
            var tools = converter.Convert(swaggerJson).Tools;

            Console.WriteLine($"Successfully converted {tools.Count} operations to tools");

            // Create the mappings structure
            var mappingsRoot = new
            {
                Mcpifier = new McpifierOptions
                {
                    Tools = tools
                }
            };

            // Serialize to JSON with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(mappingsRoot, options);

            // Write to file
            File.WriteAllText(outputFilePath, json);

            Console.WriteLine($"Successfully wrote mappings.json to: {outputFilePath}");
            Console.WriteLine($"Generated {tools.Count} tool(s)");
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine($"Please ensure swagger.json exists in the current directory: {Directory.GetCurrentDirectory()}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating mappings: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}