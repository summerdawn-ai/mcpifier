using Microsoft.Extensions.DependencyInjection;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Service builder for Mcpifier.
/// </summary>
/// <remarks>
/// Provides a surface for other libraries to extend Mcpifier
/// with additional services using extension methods.
/// </remarks>
public class McpifierBuilder(IServiceCollection services)
{
    /// <summary>
    /// Gets the services collection being configured.
    /// </summary>
    public IServiceCollection Services { get; } = services;
}
