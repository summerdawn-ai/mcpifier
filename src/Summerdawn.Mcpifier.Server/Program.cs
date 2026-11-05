using System.CommandLine;
using System.Net;

using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Main program class for the Mcpifier server.
/// </summary>
public class Program
{
    private const string ReadmeUrl = "https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.Server/README.md";
    private const string ReadmeHelpText = $"For information on how to configure and run Mcpifier, see {ReadmeUrl}";

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

        // Create shared options, but don't make them global so that
        // we can control the order in which the options are listed.
        var settingsOption = new Option<string[]>("--settings")
        {
            Description = "The path to any additional configuration JSON file to load",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true,
        };

        var noDefaultSettingsOption = new Option<bool>("--no-default-settings")
        {
            Description = "Skip loading default settings",
            Arity = ArgumentArity.Zero,
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose logging",
            Arity = ArgumentArity.Zero,
        };

        // Create "serve" command
        var serveCommand = CreateServeCommand(settingsOption, noDefaultSettingsOption, verboseOption, args);

        // Create "generate" command
        var generateCommand = CreateGenerateCommand(settingsOption, noDefaultSettingsOption, verboseOption, args);

        // Create root command
        var rootCommand = new RootCommand("Mcpifier - an MCP-to-REST gateway that can run in HTTP or stdio mode")
        {
            serveCommand,
            generateCommand
        };

        // Make "serve" the default command by sharing its options, validators and action.
        foreach (var option in serveCommand.Options) { rootCommand.Options.Add(option); }
        foreach (var validator in serveCommand.Validators) { rootCommand.Validators.Add(validator); }
        rootCommand.Action = serveCommand.Action;

        // Add custom help text to root and all child commands.
        rootCommand.AddHelpText(ReadmeHelpText);

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

    private static Command CreateServeCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, Option<bool> verboseOption, string[] args)
    {
        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "The server mode to use",
            Required = true
        }.AcceptOnlyFromAmong("http", "stdio");

        var portOption = new Option<int?>("--port")
        {
            Description = "The localhost HTTP port to use (0-65535); defaults to the first available port starting at 5157"
        };

        var swaggerOption = new Option<string>("--swagger")
        {
            Description = "The path or URL of a Swagger/OpenAPI specification JSON file from which to generate tool mappings",
            Required = false
        };

        var mappingsOption = new Option<string>("--mappings")
        {
            Description = "The path to a tool mappings JSON file from which to load tool mappings",
            Required = false
        };

        var serveCommand = new Command("serve", "Start the Mcpifier server in HTTP or stdio mode")
        {
            modeOption,
            portOption,
            swaggerOption,
            mappingsOption,
            settingsOption,
            noDefaultSettingsOption,
            verboseOption
        };

        serveCommand.Validators.Add(parseResult =>
        {
            bool swagger = parseResult.GetValue(swaggerOption) is not null;
            bool mappings = parseResult.GetValue(mappingsOption) is not null;

            // Validate that either --mappings or --swagger is specified, not both
            if (swagger && mappings)
            {
                parseResult.AddError("Cannot use both --swagger and --mappings options together");
            }

            // Validate --port only if it is specified and valid
            var portResult = parseResult.GetResult(portOption);
            if (portResult is null || portResult.Tokens.Count != 1 || !int.TryParse(portResult.Tokens[0].Value, out int port))
            {
                return;
            }

            if (parseResult.GetValue(modeOption) != "http")
            {
                parseResult.AddError("--port can only be used with --mode http.");
                return;
            }

            if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            {
                parseResult.AddError("The HTTP port must be between 0 and 65535.");
                return;
            }
        });

        serveCommand.SetAction(async parseResult =>
        {
            string mode = parseResult.GetValue(modeOption)!;
            int? port = parseResult.GetValue(portOption);
            string? swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption);
            string? mappingsFileName = parseResult.GetValue(mappingsOption);
            string[] settingsFileNames = parseResult.GetValue(settingsOption)!;
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);
            bool verboseSettings = parseResult.GetValue(verboseOption);

            await ServeAsync(mode, port, swaggerFileNameOrUrl, mappingsFileName, settingsFileNames, noDefaultSettings, verboseSettings, args);
        });

        return serveCommand;
    }

    private static Command CreateGenerateCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, Option<bool> verboseOption, string[] args)
    {
        var swaggerOption = new Option<string>("--swagger")
        {
            Description =
                "The path or URL of the Swagger/OpenAPI specification JSON file from which to generate tool mappings",
            Required = true
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output path for the generated mapping file [default: mappings.json]",
            Required = false
        };

        var generateCommand = new Command("generate", "Generate tool mappings from a Swagger/OpenAPI specification")
        {
            swaggerOption,
            outputOption,
            settingsOption,
            noDefaultSettingsOption,
            verboseOption
        };

        generateCommand.SetAction(async parseResult =>
        {
            string swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption)!;
            string? outputFileName = parseResult.GetValue(outputOption);
            string[] settingsFileNames = parseResult.GetValue(settingsOption)!;
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);
            bool verboseSettings = parseResult.GetValue(verboseOption);

            await GenerateAsync(swaggerFileNameOrUrl, outputFileName, settingsFileNames, noDefaultSettings, verboseSettings, args);
        });
        return generateCommand;
    }

    /// <summary>
    /// Starts the Mcpifier server in the specified mode.
    /// </summary>
    /// <param name="mode">The value for the `--mode` option.</param>
    /// <param name="port">The optional value for the `--port` option.</param>
    /// <param name="swaggerFileNameOrUrl">The optional value for the `--swagger` option.</param>
    /// <param name="mappingsFileName">The optional value for the `--mappings` option.</param>
    /// <param name="settingsFileNames">The optional values for the `--settings` option.</param>
    /// <param name="noDefaultSettings">The value for the `--no-default-settings` option.</param>
    /// <param name="verboseSettings">The value for the `--verbose` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task ServeAsync(string mode, int? port, string? swaggerFileNameOrUrl, string? mappingsFileName, string[] settingsFileNames, bool noDefaultSettings, bool verboseSettings, string[] args)
    {
        if (mode == "http")
        {
            WebApplication app;
            try
            {
                // Load appsettings.json from the content directory if present.
                var builder = WebApplication.CreateBuilder();

                // Load embedded default and custom settings.
                builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, verboseSettings);

                bool hasConfiguredEndpoints = builder.Configuration.GetSection("Kestrel:Endpoints").GetChildren().Any();
                if (hasConfiguredEndpoints && port.HasValue)
                {
                    throw new InvalidOperationException("--port cannot be used with configured Kestrel:Endpoints.");
                }

                if (!hasConfiguredEndpoints)
                {
                    string listenerUrl = $"http://localhost:{ServerPortSelector.Select(port)}";
                    builder.WebHost.UseUrls(listenerUrl);
                }
                else
                {
                    // Configured endpoints take precedence over host URL settings.
                    builder.Configuration[WebHostDefaults.PreferHostingUrlsKey] = "false";
                }

                // Configure HTTP MCP gateway.
                var mcpifierBuilder = builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier")).AddAspNetCore();

                // Add tools from local mappings.json if present -
                // even if explicit mappings/Swagger is specified.
                const string localMappingsFileName = "mappings.json";
                if (File.Exists(localMappingsFileName))
                {
                    mcpifierBuilder.AddToolsFromMappings(localMappingsFileName);
                }

                // Add tools from mappings or Swagger if specified.
                if (mappingsFileName is not null)
                {
                    mcpifierBuilder.AddToolsFromMappings(mappingsFileName);
                }
                if (swaggerFileNameOrUrl is not null)
                {
                    mcpifierBuilder.AddToolsFromSwagger(swaggerFileNameOrUrl);
                }

                // Configure CORS to allow any connection.
                builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

                app = builder.Build();

                app.UseRouting();
                app.UseCors();

                // Use HTTP MCP gateway.
                app.MapMcpifier();
            }
            catch (DirectoryNotFoundException ex)
            {
                // Special-case DirectoryNotFoundException with missing error message.
                throw new InvalidOperationException($"Directory not found: {ex.Message}\n\n{ReadmeHelpText}", ex);
            }
            catch (Exception ex)
            {
                // Append note about README.md to exceptions thrown during app configuration.
                throw new InvalidOperationException($"{ex.Message}\n\n{ReadmeHelpText}", ex);
            }

            await app.RunAsync();
        }
        else
        {
            IHost app;
            try
            {
                var builder = Host.CreateApplicationBuilder(args);

                // Load embedded, default and custom settings and mappings files.
                builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, verboseSettings);

                // Configure stdio MCP gateway.
                var mcpifierBuilder = builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

                // Add tools from local mappings.json if present -
                // even if explicit mappings/Swagger is specified.
                const string localMappingsFileName = "mappings.json";
                if (File.Exists(localMappingsFileName))
                {
                    mcpifierBuilder.AddToolsFromMappings(localMappingsFileName);
                }

                // Add tools from mappings or Swagger if specified.
                if (mappingsFileName is not null)
                {
                    mcpifierBuilder.AddToolsFromMappings(mappingsFileName);
                }
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
            catch (DirectoryNotFoundException ex)
            {
                // Special-case DirectoryNotFoundException with missing error message.
                throw new InvalidOperationException($"Directory not found: {ex.Message}\n\n{ReadmeHelpText}", ex);
            }
            catch (Exception ex)
            {
                // Append note about README.md to exceptions thrown during app configuration.
                throw new InvalidOperationException($"{ex.Message}\n\n{ReadmeHelpText}", ex);
            }

            await app.RunAsync();
        }
    }

    /// <summary>
    /// Generates Mcpifier tool mappings from the specified Swagger specification.
    /// </summary>
    /// <param name="swaggerFileNameOrUrl">The value for the `--swagger` option.</param>
    /// <param name="outputFileName">The optional value for the `--output` option.</param>
    /// <param name="settingsFileNames">The optional values for the `--settings` option.</param>
    /// <param name="noDefaultSettings">The value for the `--no-default-settings` option.</param>
    /// <param name="verboseSettings">The value for the `--verbose` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task GenerateAsync(string swaggerFileNameOrUrl, string? outputFileName, string[] settingsFileNames, bool noDefaultSettings, bool verboseSettings, string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load embedded, default and custom settings and mappings files.
            builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, verboseSettings);

            builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

            var app = builder.Build();

            var converter = app.Services.GetRequiredService<SwaggerConverter>();

            await converter.LoadAndConvertAsync(swaggerFileNameOrUrl, outputFileName);
        }
        catch (DirectoryNotFoundException ex)
        {
            // Special-case DirectoryNotFoundException with missing error message.
            throw new InvalidOperationException($"Directory not found: {ex.Message}\n\n{ReadmeHelpText}", ex);
        }
        catch (Exception ex)
        {
            // Append note about README.md to exceptions thrown during app configuration.
            throw new InvalidOperationException($"{ex.Message}\n\n{ReadmeHelpText}", ex);
        }
    }
}
