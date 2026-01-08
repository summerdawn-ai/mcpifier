using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="McpifierBuilder"/> to add JSON mappings file support.
/// </summary>
public static class MappingsMcpifierBuilderExtensions
{
    /// <summary>
    /// Adds the specified JSON mappings file as a source of Mcpifier tool mappings.
    /// </summary>
    /// <param name="mcpifierBuilder">The <see cref="McpifierBuilder"/> instance.</param>
    /// <param name="fileName">The file name of the mappings JSON file.</param>
    /// <returns>The builder instance.</returns>
    public static McpifierBuilder AddToolsFromMappings(this McpifierBuilder mcpifierBuilder, string fileName)
    {
        // Add mappings file as configuration source, but don't process it yet.
        mcpifierBuilder.Services.AddSingleton(new MappingsConfigurationSource(fileName));

        // Add post-configure loader to load and merge mappings when options are first resolved.
        mcpifierBuilder.Services.AddSingleton<IPostConfigureOptions<McpifierOptions>, MappingsConfigurationLoader>();

        return mcpifierBuilder;
    }
}
