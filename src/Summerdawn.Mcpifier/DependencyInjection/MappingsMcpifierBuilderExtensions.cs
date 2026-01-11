using Microsoft.Extensions.DependencyInjection;

namespace Summerdawn.Mcpifier.DependencyInjection;

public static class MappingsMcpifierBuilderExtensions
{
    /// <summary>
    /// Adds the specified JSON mappings file as a source of Mcpifier tool mappings.
    /// </summary>
    public static McpifierBuilder AddToolsFromMappings(this McpifierBuilder mcpifierBuilder, string fileName)
    {
        mcpifierBuilder.Services.AddSingleton(new MappingsConfigurationSource(fileName));
        mcpifierBuilder.Services.AddSingleton<IPostConfigureOptions<Configuration.McpifierOptions>, MappingsConfigurationLoader>();
        return mcpifierBuilder;
    }
}
