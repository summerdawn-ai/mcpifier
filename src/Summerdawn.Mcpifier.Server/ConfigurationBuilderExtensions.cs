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
    public static void AddMcpifierSettings(this IConfigurationBuilder configurationBuilder, bool noDefaultSettings, string[] settingsFileNames, bool verboseSettings)
    {
        // Load embedded appsettings.json as first configuration source (unless disabled)
        if (!noDefaultSettings)
        {
            configurationBuilder.AddJsonResource("appsettings.json", position: 0);
        }

        // Load custom appsettings.json if specified
        configurationBuilder.AddJsonFiles(settingsFileNames);

        if (verboseSettings)
        {
            configurationBuilder.AddJsonResource("appsettings.Verbose.json");
        }
    }

    /// <summary>
    /// Adds the specified embedded resource as the first configuration source.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add the source to.</param>
    /// <param name="resourceName">The name of the resource in the executing assembly.</param>
    /// <param name="position">The optional insertion index.</param>
    public static IConfigurationBuilder AddJsonResource(this IConfigurationBuilder configurationBuilder, string resourceName, int? position = null)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var resourceStream = assembly.GetManifestResourceStream($"{ResourceNamespace}.{resourceName}") ??
                                   throw new ArgumentException($"Resource {resourceName} not found in assembly.");

        // Copy to MemoryStream for configuration system use.
        // NOTE: The MemoryStream is intentionally NOT disposed here. The JsonStreamConfigurationProvider
        // takes ownership of the stream and will dispose it when the provider itself is disposed as part
        // of the configuration system's lifecycle.
        // This is the standard pattern for stream-based configuration sources.
        var memoryStream = new MemoryStream();
        resourceStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var source = new JsonStreamConfigurationSource
        {
            Stream = memoryStream
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
    /// Adds the specified settings files to the configuration.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add the sources to.</param>
    /// <param name="path">The JSON file path.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="position">The optional insertion index.</param>
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
    /// Adds the specified settings files to the configuration.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add the sources to.</param>
    /// <param name="paths">The JSON file paths.</param>
    public static IConfigurationBuilder AddJsonFiles(this IConfigurationBuilder configurationBuilder, IEnumerable<string> paths)
    {
        foreach (string settingsFile in paths)
        {
            configurationBuilder.AddJsonFile(settingsFile, optional: false);
        }

        return configurationBuilder;
    }
}
