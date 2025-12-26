using System.CommandLine;

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
            args = ["serve", "--mode=http"];
        }

        // Create "serve" command
        var serveCommand = CreateServeCommand(args);

        // Create "generate" command
        var generateCommand = CreateGenerateCommand(args);

        // Create root command
        var rootCommand = new RootCommand("Mcpifier - an MCP-to-REST gateway that can run in HTTP or stdio mode")
        {
            serveCommand,
            generateCommand
        };

        // Make "serve" the default command by adding its options to root and setting the same action.
        foreach (var option in serveCommand.Options) rootCommand.Add(option);
        rootCommand.Action = serveCommand.Action;

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

    private static Command CreateServeCommand(string[] args)
    {
        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "The server mode to use.",
            Required = true
        }.AcceptOnlyFromAmong("http", "stdio");

        var swaggerOption = new Option<string>("--swagger")
        {
            Description =
                "The optional file name or URL of the Swagger/OpenAPI specification JSON file from which to generate tool mappings.",
            Required = false
        };

        var serveCommand = new Command("serve", "Start the Mcpifier server in HTTP or stdio mode")
        {
            modeOption,
            swaggerOption
        };

        serveCommand.SetAction(async parseResult =>
        {
            string mode = parseResult.GetValue(modeOption)!;
            string? swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption);

            await ServeAsync(mode, swaggerFileNameOrUrl, args);
        });

        return serveCommand;
    }

    private static Command CreateGenerateCommand(string[] args)
    {
        var swaggerOption = new Option<string>("--swagger")
        {
            Description =
                "The file name or URL of the Swagger/OpenAPI specification JSON file from which to generate tool mappings.",
            Required = true
        };

        var outputOption = new Option<string?>("--output")
        {
            Description = "The optional custom file name for the generated mappings.",
            Required = false
        };

        var generateCommand = new Command("generate", "Generate tool mappings from a Swagger/OpenAPI specification")
        {
            swaggerOption,
            outputOption
        };
        generateCommand.SetAction(async parseResult =>
        {
            string swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption)!;
            string? outputFileName = parseResult.GetValue(outputOption);

            await GenerateAsync(swaggerFileNameOrUrl, outputFileName, args);
        });
        return generateCommand;
    }

    /// <summary>
    /// Starts the Mcpifier server in the specified mode.
    /// </summary>
    /// <param name="mode">The value for the `--mode` option.</param>
    /// <param name="swaggerFileNameOrUrl">The optional value for the `--swagger` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task ServeAsync(string mode, string? swaggerFileNameOrUrl, string[] args)
    {
        if (mode == "http")
        {
            WebApplication app;
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Load tool mappings from separate file.
                // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                builder.Configuration.AddJsonFile("mappings.json", optional: true);

                // Configure HTTP MCP gateway.
                var mcpifierBuilder = builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier")).AddAspNetCore();

                if (swaggerFileNameOrUrl is not null)
                {
                    mcpifierBuilder.AddToolsFromSwagger(swaggerFileNameOrUrl);
                }

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
                // Append note about README.md to exceptions thrown during app configuration.
                throw new InvalidOperationException($"{ex.Message}\r\n\r\nConsult README.md for instructions on how to configure Mcpifier.", ex);
            }

            await app.RunAsync();
        }
        else
        {
            IHost app;
            try
            {
                var builder = Host.CreateApplicationBuilder(args);

                // Load tool mappings from separate file.
                // Set DOTNET_CONTENTROOT environment variable if the file is _not_ in the current working directory.
                builder.Configuration.AddJsonFile("mappings.json", optional: true);

                // Configure stdio MCP gateway.
                var mcpifierBuilder = builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

                if (swaggerFileNameOrUrl is not null)
                {
                    mcpifierBuilder.AddToolsFromSwagger(swaggerFileNameOrUrl);
                }

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
                // Append note about README.md to exceptions thrown during app configuration.
                throw new InvalidOperationException($"{ex.Message}\r\n\r\nConsult README.md for instructions on how to configure Mcpifier.", ex);
            }

            await app.RunAsync();
        }
    }

    /// <summary>
    /// Generates Mcpifier tool mappings from the specified Swagger specification.
    /// </summary>
    /// <param name="swaggerFileNameOrUrl">The value for the `--swagger` option.</param>
    /// <param name="outputFileName">The optional value for the `--output` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task GenerateAsync(string swaggerFileNameOrUrl, string? outputFileName, string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

            var app = builder.Build();

            var converter = app.Services.GetRequiredService<SwaggerConverter>();

            await converter.LoadAndConvertAsync(swaggerFileNameOrUrl, outputFileName);
        }
        catch (Exception ex)
        {
            // Append note about README.md to exceptions thrown during app configuration.
            throw new InvalidOperationException($"{ex.Message}\r\n\r\nConsult README.md for instructions on how to configure Mcpifier.", ex);
        }
    }
}