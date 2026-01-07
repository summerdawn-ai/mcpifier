using System.Reflection;

using Microsoft.Extensions.Configuration.Json;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Extension methods for <see cref="ConfigurationManager"/>.
/// </summary>
internal static class ConfigurationManagerExtensions
{
    private const string ResourceNamespace = "Summerdawn.Mcpifier.Server";

    /// <summary>
    /// Adds Mcpifier settings from various sources to the configuration.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the sources to.</param>
    /// <param name="noDefaultSettings">Whether to skip loading embedded default settings.</param>
    /// <param name="settingsFileNames">Array of settings file paths to load.</param>
    /// <param name="mappingsFileName">Optional custom mappings file name to load.</param>
    public static void AddMcpifierSettings(this ConfigurationManager configurationManager, bool noDefaultSettings, string[] settingsFileNames, string? mappingsFileName)
    {
        // Load embedded appsettings.json as first configuration source (unless disabled)
        if (!noDefaultSettings)
        {
            configurationManager.AddJsonResource("appsettings.json");
        }

        // Load mappings.json from working directory if present
        configurationManager.AddJsonFile("mappings.json", optional: true);

        // Load custom appsettings.json if specified
        configurationManager.AddJsonFiles(settingsFileNames);

        // Load custom mappings.json if specified 
        if (mappingsFileName is not null)
        {
            configurationManager.AddJsonFile(mappingsFileName, optional: false);
        }
    }

    /// <summary>
    /// Adds the specified embedded resource as the first configuration source.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the source to.</param>
    /// <param name="resourceName">The name of the resource in the executing assembly.</param>
    public static void AddJsonResource(this ConfigurationManager configurationManager, string resourceName)
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

        // Insert at position 0 to make it the first source
        configurationManager.Sources.Insert(0, new JsonStreamConfigurationSource
        {
            Stream = memoryStream
        });
    }

    /// <summary>
    /// Adds the specified settings files to the configuration.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the sources to.</param>
    /// <param name="paths">Array of settings file paths to load.</param>
    public static void AddJsonFiles(this ConfigurationManager configurationManager, string[] paths)
    {
        foreach (string settingsFile in paths)
        {
            configurationManager.AddJsonFile(settingsFile, optional: false);
        }
    }
}
