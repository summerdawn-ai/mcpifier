using Microsoft.Extensions.DependencyInjection;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="McpifierBuilder"/> to add mappings loading.
/// </summary>
public static class MappingsMcpifierBuilderExtensions
{
    /// <summary>
    /// Adds the specified tool mappings JSON file as a source of Mcpifier tool mappings and configuration.
    /// </summary>
    /// <remarks>
    /// Specifically, loads tool mappings and sets the REST API base address, if not already defined.
    /// </remarks>
    /// <param name="mcpifierBuilder">The <see cref="McpifierBuilder"/> instance.</param>
    /// <param name="fileName">The file name of the tool mappings JSON file.</param>
    /// <returns>The builder instance.</returns>
    public static McpifierBuilder AddToolsFromMappings(this McpifierBuilder mcpifierBuilder, string fileName)
    {
        // Add mappings file as configuration source, but don't process it yet.
        mcpifierBuilder.Services.AddSingleton(new MappingsConfigurationSource(fileName));

        // Add post-configure loader to load and merge mappings tools when options are first resolved.
        mcpifierBuilder.Services.AddSingleton<IPostConfigureOptions<Configuration.McpifierOptions>, MappingsConfigurationLoader>();

        return mcpifierBuilder;
    }
}
