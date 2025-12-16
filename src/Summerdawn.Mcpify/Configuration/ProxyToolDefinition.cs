using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Configuration;

/// <summary>
/// Defines a proxy tool that maps an MCP tool to a REST API endpoint.
/// </summary>
public class ProxyToolDefinition
{
    /// <summary>
    /// Gets or sets the MCP tool definition.
    /// </summary>
    public required McpToolDefinition Mcp { get; set; }

    /// <summary>
    /// Gets or sets the REST API configuration for this tool.
    /// </summary>
    public required RestConfiguration Rest { get; set; }
}

/// <summary>
/// Configuration for REST API endpoint.
/// </summary>
public class RestConfiguration
{
    /// <summary>
    /// Gets or sets the HTTP method for the REST API call.
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Gets or sets the REST API path with optional parameter placeholders.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query string with optional parameter placeholders.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the request body with optional parameter placeholders.
    /// </summary>
    public string? Body { get; set; }
}
