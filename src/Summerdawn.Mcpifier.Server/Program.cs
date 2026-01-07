using System.CommandLine;

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

        // Create "serve" command
        var serveCommand = CreateServeCommand(settingsOption, noDefaultSettingsOption, args);

        // Create "generate" command
        var generateCommand = CreateGenerateCommand(settingsOption, noDefaultSettingsOption, args);

        // Create root command
        var rootCommand = new RootCommand("Mcpifier - an MCP-to-REST gateway that can run in HTTP or stdio mode")
        {
            serveCommand,
            generateCommand
        };

        // Make "serve" the default command by adding its options to root and setting the same action.
        foreach (var option in serveCommand.Options) { rootCommand.Options.Add(option); }
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

    private static Command CreateServeCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, string[] args)
    {
        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "The server mode to use",
            Required = true
        }.AcceptOnlyFromAmong("http", "stdio");

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
            swaggerOption,
            mappingsOption,
            settingsOption,
            noDefaultSettingsOption
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
        });

        serveCommand.SetAction(async parseResult =>
        {
            string mode = parseResult.GetValue(modeOption)!;
            string? swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption);
            string? mappingsFileName = parseResult.GetValue(mappingsOption);
            string[] settingsFileNames = parseResult.GetValue(settingsOption)!;
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);

            await ServeAsync(mode, swaggerFileNameOrUrl, mappingsFileName, settingsFileNames, noDefaultSettings, args);
        });

        return serveCommand;
    }

    private static Command CreateGenerateCommand(Option<string[]> settingsOption, Option<bool> noDefaultSettingsOption, string[] args)
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
            noDefaultSettingsOption
        };

        generateCommand.SetAction(async parseResult =>
        {
            string swaggerFileNameOrUrl = parseResult.GetValue(swaggerOption)!;
            string? outputFileName = parseResult.GetValue(outputOption);
            string[] settingsFileNames = parseResult.GetValue(settingsOption)!;
            bool noDefaultSettings = parseResult.GetValue(noDefaultSettingsOption);

            await GenerateAsync(swaggerFileNameOrUrl, outputFileName, settingsFileNames, noDefaultSettings, args);
        });
        return generateCommand;
    }

    /// <summary>
    /// Starts the Mcpifier server in the specified mode.
    /// </summary>
    /// <param name="mode">The value for the `--mode` option.</param>
    /// <param name="swaggerFileNameOrUrl">The optional value for the `--swagger` option.</param>
    /// <param name="mappingsFileName">The optional value for the `--mappings` option.</param>
    /// <param name="settingsFileNames">The optional values for the `--settings` option.</param>
    /// <param name="noDefaultSettings">The value for the `--no-default-settings` option.</param>
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task ServeAsync(string mode, string? swaggerFileNameOrUrl, string? mappingsFileName, string[] settingsFileNames, bool noDefaultSettings, string[] args)
    {
        if (mode == "http")
        {
            WebApplication app;
            try
            {
                // Loads appsettings.json from working directory if present
                var builder = WebApplication.CreateBuilder();

                // Load embedded, default and custom settings and mappings files.
                builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, mappingsFileName);

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
                builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, mappingsFileName);

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
    /// <param name="args">The collection of command-line arguments.</param>
    private static async Task GenerateAsync(string swaggerFileNameOrUrl, string? outputFileName, string[] settingsFileNames, bool noDefaultSettings, string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Load embedded, default and custom settings and mappings files.
            builder.Configuration.AddMcpifierSettings(noDefaultSettings, settingsFileNames, mappingsFileName: null);

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
