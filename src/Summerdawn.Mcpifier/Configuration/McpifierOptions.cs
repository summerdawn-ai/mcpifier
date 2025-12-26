using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Configuration;

/// <summary>
/// Configuration options for Mcpifier.
/// </summary>
public class McpifierOptions
{
    /// <summary>
    /// Gets or sets the REST API configuration.
    /// </summary>
    public McpifierRestSection Rest { get; set; } = new();

    /// <summary>
    /// Gets or sets the MCP protocol version.
    /// </summary>
    public string ProtocolVersion { get; set; } = "2025-06-18";

    /// <summary>
    /// Gets or sets the MCP server information.
    /// </summary>
    public McpServerInfo ServerInfo { get; set; } = new();

    /// <summary>
    /// Gets or sets optional instructions for MCP clients.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets the list of tool definitions.
    /// </summary>
    public List<McpifierToolMapping> Tools { get; set; } = [];

    /// <summary>
    /// Gets or sets the authorization configuration.
    /// </summary>
    public McpifierAuthorizationSection Authorization { get; set; } = new();
}

/// <summary>
/// Configuration for REST API communication.
/// </summary>
public class McpifierRestSection
{
    /// <summary>
    /// Gets or sets the base address for REST API calls. Can be absolute or relative.
    /// </summary>
    public string BaseAddress { get; set; } = "";

    /// <summary>
    /// Gets or sets default HTTP headers to include in all REST API requests.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];

    /// <summary>
    /// Gets or sets headers to forward from incoming MCP requests to REST API calls.
    /// </summary>
    public Dictionary<string, bool> ForwardedHeaders { get; set; } = [];
}

/// <summary>
/// Configuration for MCP authorization.
/// </summary>
public class McpifierAuthorizationSection
{
    /// <summary>
    /// Gets or sets a value indicating whether authorization is required for MCP requests.
    /// </summary>
    public bool RequireAuthorization { get; set; } = false;

    /// <summary>
    /// Gets or sets the protected resource metadata for OAuth authorization.
    /// </summary>
    public ProtectedResourceMetadata? ResourceMetadata { get; set; }
}