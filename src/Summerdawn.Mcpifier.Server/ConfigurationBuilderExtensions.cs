using System.Reflection;

using Microsoft.Extensions.Configuration.Json;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder"/>.
/// </summary>
internal static class ConfigurationBuilderExtensions
{
    private const string ResourceNamespace = "Summerdawn.Mcpifier.Server";

    /// <summary>
    /// Adds Mcpifier settings from various sources to the configuration.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add the sources to.</param>
    /// <param name="noDefaultSettings">Whether to skip loading embedded default settings.</param>
    /// <param name="settingsFileNames">Array of settings file paths to load.</param>
    /// <param name="verboseSettings">Whether to load the embedded verbose logging settings last.</param>
    /// <returns>The same configuration builder so additional sources can be chained.</returns>
    public static IConfigurationBuilder AddMcpifierSettings(this IConfigurationBuilder configurationBuilder, bool noDefaultSettings, string[] settingsFileNames, bool verboseSettings)
    {
        if (!noDefaultSettings)
        {
            configurationBuilder.AddJsonResource("appsettings.Default.json", position: 0);
        }

        if (settingsFileNames.Length > 0)
        {
            // Host builders already add environment variables; keep settings files below that source.
            int? environmentVariablesPosition = configurationBuilder.GetEnvironmentVariablesPosition();
            configurationBuilder.AddJsonFiles(settingsFileNames, environmentVariablesPosition);
        }

        if (verboseSettings)
        {
            configurationBuilder.AddJsonResource("appsettings.Verbose.json");
        }

        return configurationBuilder;
    }

    /// <summary>
    /// Adds an embedded JSON resource to the configuration pipeline.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to update.</param>
    /// <param name="resourceName">The embedded resource file name.</param>
    /// <param name="position">The optional insertion index.</param>
    /// <returns>The same configuration builder so additional sources can be chained.</returns>
    public static IConfigurationBuilder AddJsonResource(this IConfigurationBuilder configurationBuilder, string resourceName, int? position = null)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var resourceStream = assembly.GetManifestResourceStream($"{ResourceNamespace}.{resourceName}")
            ?? throw new ArgumentException($"Resource '{resourceName}' not found in assembly.");

        // Copy to MemoryStream for configuration system use.
        // NOTE: ConfigurationManager may dispose and then reload inserted stream sources.
        // Use NeverClosingMemoryStream so the embedded JSON source survives later source insertion.
        var memoryStream = new NeverClosingMemoryStream();
        resourceStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var source = new JsonStreamConfigurationSource
        {
            Stream = memoryStream,
        };

        if (position.HasValue)
        {
            configurationBuilder.Sources.Insert(position.Value, source);
        }
        else
        {
            configurationBuilder.Sources.Add(source);
        }

        return configurationBuilder;
    }

    /// <summary>
    /// Adds a JSON file to the configuration pipeline.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to update.</param>
    /// <param name="path">The JSON file path.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="position">The optional insertion index.</param>
    /// <returns>The same configuration builder so additional sources can be chained.</returns>
    public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder configurationBuilder, string path, bool optional = false, int? position = null)
    {
        var source = new JsonConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = false,
        };

        source.ResolveFileProvider();

        if (position.HasValue)
        {
            configurationBuilder.Sources.Insert(position.Value, source);
        }
        else
        {
            configurationBuilder.Sources.Add(source);
        }

        return configurationBuilder;
    }

    /// <summary>
    /// Adds JSON files to the configuration pipeline.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to update.</param>
    /// <param name="paths">The JSON file paths.</param>
    /// <param name="position">The optional insertion index for the first file.</param>
    /// <returns>The same configuration builder so additional sources can be chained.</returns>
    public static IConfigurationBuilder AddJsonFiles(this IConfigurationBuilder configurationBuilder, IEnumerable<string> paths, int? position = null)
    {
        int? currentPosition = position;

        foreach (string path in paths)
        {
            configurationBuilder.AddJsonFile(path, optional: false, currentPosition);

            if (currentPosition.HasValue)
            {
                currentPosition++;
            }
        }

        return configurationBuilder;
    }

    /// <summary>
    /// Gets the position of the environment variables configuration source.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to inspect.</param>
    /// <returns>The source position, or <see langword="null"/> when no matching source exists.</returns>
    private static int? GetEnvironmentVariablesPosition(this IConfigurationBuilder configurationBuilder)
    {
        int position = configurationBuilder.Sources
            .ToList()
            .FindIndex(source => source.GetType().Name == "EnvironmentVariablesConfigurationSource");

        return position < 0 ? null : position;
    }

    private sealed class NeverClosingMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            Seek(0, SeekOrigin.Begin);
        }

        public override ValueTask DisposeAsync()
        {
            Seek(0, SeekOrigin.Begin);
            return default;
        }
    }
}
