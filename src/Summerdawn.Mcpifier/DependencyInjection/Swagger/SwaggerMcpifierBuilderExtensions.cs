using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="McpifierBuilder"/> to add Swagger/OpenAPI support.
/// </summary>
public static class SwaggerMcpifierBuilderExtensions
{
    /// <summary>
    /// Adds the specified Swagger/OpenAPI specification file as a source of Mcpifier tool mappings and configuration.
    /// </summary>
    /// <remarks>
    /// Specifically, creates tool mappings for all endpoints in the specification and sets the REST API base address,
    /// if undefined, to server address or host of the specification.
    /// </remarks>
    /// <param name="mcpifierBuilder">The <see cref="McpifierBuilder"/> instance.</param>
    /// <param name="fileNameOrUrl">The file name or URL of the Swagger/OpenAPI specification.</param>
    /// <param name="mappingAction">The optional action to apply to each loaded tool mapping.</param>
    /// <param name="mappingPredicate">The optional predicate to filter loaded tool mappings.</param>
    /// <returns>The builder instance.</returns>
    public static McpifierBuilder AddToolsFromSwagger(this McpifierBuilder mcpifierBuilder, string fileNameOrUrl, Action<McpifierToolMapping>? mappingAction = null, Func<McpifierToolMapping, bool>? mappingPredicate = null)
    {
        // Add Swagger file as configuration source, but don't process it yet.
        mcpifierBuilder.Services.AddSingleton(new SwaggerConfigurationSource(fileNameOrUrl, mappingAction, mappingPredicate));

        // Add post-configure loader to load and merge Swagger tools when options are first resolved.
        mcpifierBuilder.Services.AddSingleton<IPostConfigureOptions<McpifierOptions>, SwaggerConfigurationLoader>();

        return mcpifierBuilder;
    }
}
