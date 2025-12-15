using Microsoft.Extensions.DependencyInjection;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Service builder for Mcpify.
/// </summary>
/// <remarks>
/// Provides a surface for other libraries to extend Mcpify
/// with additional services using extension methods.
/// </remarks>
public class McpifyBuilder(IServiceCollection services)
{
    /// <summary>
    /// Gets the services collection being configured.
    /// </summary>
    public IServiceCollection Services { get; } = services;
}