using System.CommandLine;

using Summerdawn.Mcpify.DependencyInjection;

namespace Summerdawn.Mcpify.Server;

public  class Program
{
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