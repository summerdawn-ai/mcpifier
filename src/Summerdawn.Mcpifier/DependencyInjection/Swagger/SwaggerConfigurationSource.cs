using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Defines a source of Swagger/OpenAPI tool mappings for Mcpifier.
/// </summary>
internal record SwaggerConfigurationSource(string FileNameOrUrl, Action<McpifierToolMapping>? MappingAction, Func<McpifierToolMapping, bool>? MappingFilter);