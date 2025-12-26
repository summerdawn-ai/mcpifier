using System.Text.Json.Serialization;

using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Configuration;

/// <summary>
/// Defines a mapping from an MCP tool to a REST API endpoint.
/// </summary>
public class McpifierToolMapping
{
    /// <summary>
    /// Gets or sets the MCP tool definition.
    /// </summary>
    public McpToolDefinition Mcp { get; set; } = new();

    /// <summary>
    /// Gets or sets the REST API configuration for this tool.
    /// </summary>
    public RestConfiguration Rest { get; set; } = new();
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the request body with optional parameter placeholders.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; set; }
}
