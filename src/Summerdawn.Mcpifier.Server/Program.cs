using System.CommandLine;
using System.Reflection;

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

        // Create global options
        var settingsOption = new Option<string[]>("--settings")
        {
            Description = "Path to a JSON configuration file to load. Can be specified multiple times.",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = false
        };

        var noDefaultSettingsOption = new Option<bool>("--no-default-settings")
        {
            Description = "Do not load the default embedded appsettings.json.",
            Arity = ArgumentArity.Zero
        };

        // Create "serve" command
        var serveCommand = CreateServeCommand(settingsOption, noDefaultSettingsOption, args);

        // Create "generate" command
        var generateCommand = CreateGenerateCommand(settingsOption, noDefaultSettingsOption, args);

        // Create root command with global options
        var rootCommand = new RootCommand("Mcpifier - an MCP-to-REST gateway that can run in HTTP or stdio mode")
        {
            settingsOption,
            noDefaultSettingsOption,
            serveCommand,
            generateCommand
        };

        // Make "serve" the default command by adding its options to root and setting the same action.
        // Skip global options that are already added to root command.
        foreach (var option in serveCommand.Options)
        {
            if (option != settingsOption && option != noDefaultSettingsOption)
            {
                rootCommand.Add(option);
            }
        }
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

    private static Command CreateServeCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, string[] args)
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

        var mappingsOption = new Option<string>("--mappings")
        {
            Description = "The file path to a JSON file containing tool mappings.",
            Required = false
        };

        var serveCommand = new Command("serve", "Start the Mcpifier server in HTTP or stdio mode")
        {
            settingsOption,
            noDefaultSettingsOption,
            modeOption,
            swaggerOption,
            mappingsOption
        };

        serveCommand.SetAction(async parseResult =>
        {
            string mode = parseResult.GetValue(modeOption)!;
            string? swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption);
            string? mappingsFile = parseResult.GetValue(mappingsOption);
            string[]? settingsFiles = parseResult.GetValue(settingsOption);
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);

            // Validate that either --mappings or --swagger is specified, not both
            if (swaggerFileNameOrUrl is not null && mappingsFile is not null)
            {
                throw new InvalidOperationException("Cannot specify both --swagger and --mappings options. Please use only one.");
            }

            await ServeAsync(mode, swaggerFileNameOrUrl, mappingsFile, settingsFiles, noDefaultSettings, args);
        });

        return serveCommand;
    }

    private static Command CreateGenerateCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, string[] args)
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
            settingsOption,
            noDefaultSettingsOption,
            swaggerOption,
            outputOption
        };
        generateCommand.SetAction(async parseResult =>
        {
            string swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption)!;
            string? outputFileName = parseResult.GetValue(outputOption);
            string[]? settingsFiles = parseResult.GetValue(settingsOption);
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);

            await GenerateAsync(swaggerFileNameOrUrl, outputFileName, settingsFiles, noDefaultSettings, args);
        });
        return generateCommand;
    }

    /// <summary>
    /// Adds the embedded appsettings.json as the first configuration source.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the source to.</param>
    private static void AddEmbeddedAppSettings(ConfigurationManager configurationManager)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Summerdawn.Mcpifier.Server.appsettings.json";
        
        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is not null)
        {
            // Copy to MemoryStream for configuration system use.
            // NOTE: The MemoryStream is intentionally NOT disposed here. The JsonStreamConfigurationProvider
            // takes ownership of the stream and will dispose it when the provider itself is disposed as part
            // of the configuration system's lifecycle. This is the standard pattern for stream-based configuration sources.
            var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            
            // Insert at position 0 to make it the first source
            configurationManager.Sources.Insert(0, new Microsoft.Extensions.Configuration.Json.JsonStreamConfigurationSource
            {
                Stream = memoryStream
            });
        }
    }

    /// <summary>
    /// Adds custom settings files to the configuration.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the sources to.</param>
    /// <param name="settingsFiles">Array of settings file paths to load.</param>
    private static void AddCustomSettings(ConfigurationManager configurationManager, string[]? settingsFiles)
    {
        if (settingsFiles is null || settingsFiles.Length == 0)
            return;

        foreach (string settingsFile in settingsFiles)
        {
            configurationManager.AddJsonFile(settingsFile, optional: false, reloadOnChange: false);
        }
    }

    /// <summary>
    /// Starts the Mcpifier server in the specified mode.
    /// </summary>
    /// <param name="mode">The value for the `--mode` option.</param>
    /// <param name="swaggerFileNameOrUrl">The optional value for the `--swagger` option.</param>
    /// <param name="mappingsFile">The optional value for the `--mappings` option.</param>
    /// <param name="settingsFiles">The optional values for the `--settings` option.</param>
    /// <param name="noDefaultSettings">The value for the `--no-default-settings` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task ServeAsync(string mode, string? swaggerFileNameOrUrl, string? mappingsFile, string[]? settingsFiles, bool noDefaultSettings, string[] args)
    {
        if (mode == "http")
        {
            WebApplication app;
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Load embedded appsettings.json as first configuration source (unless disabled)
                if (!noDefaultSettings)
                {
                    AddEmbeddedAppSettings(builder.Configuration);
                }

                // Load custom settings files (after default settings if both are present)
                AddCustomSettings(builder.Configuration, settingsFiles);

                // Load mappings file (after custom settings if specified)
                if (mappingsFile is not null)
                {
                    builder.Configuration.AddJsonFile(mappingsFile, optional: false, reloadOnChange: false);
                }

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

                // Load embedded appsettings.json as first configuration source (unless disabled)
                if (!noDefaultSettings)
                {
                    AddEmbeddedAppSettings(builder.Configuration);
                }

                // Load custom settings files (after default settings if both are present)
                AddCustomSettings(builder.Configuration, settingsFiles);

                // Load mappings file (after custom settings if specified)
                if (mappingsFile is not null)
                {
                    builder.Configuration.AddJsonFile(mappingsFile, optional: false, reloadOnChange: false);
                }

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
    /// <param name="settingsFiles">The optional values for the `--settings` option.</param>
    /// <param name="noDefaultSettings">The value for the `--no-default-settings` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task GenerateAsync(string swaggerFileNameOrUrl, string? outputFileName, string[]? settingsFiles, bool noDefaultSettings, string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load embedded appsettings.json as first configuration source (unless disabled)
            if (!noDefaultSettings)
            {
                AddEmbeddedAppSettings(builder.Configuration);
            }

            // Load custom settings files (after default settings if both are present)
            AddCustomSettings(builder.Configuration, settingsFiles);

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