using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents the result of an MCP initialize request.
/// </summary>
internal sealed class McpInitializeResult
{
    /// <summary>
    /// Gets or sets the MCP protocol version.
    /// </summary>
    public string ProtocolVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the server capabilities.
    /// </summary>
    public McpCapabilities Capabilities { get; init; } = new();

    /// <summary>
    /// Gets or sets the server information.
    /// </summary>
    public McpServerInfo ServerInfo { get; init; } = new();

    /// <summary>
    /// Gets or sets optional instructions for MCP clients.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; init; }
}

/// <summary>
/// Represents MCP server capabilities.
/// </summary>
internal sealed class McpCapabilities
{
    /// <summary>
    /// Gets or sets the completions capability.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpCompletionsCapabilities? Completions { get; init; }

    /// <summary>
    /// Gets or sets the logging capability.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpLoggingCapabilities? Logging { get; init; }

    /// <summary>
    /// Gets or sets the prompts capability.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpPromptsCapabilities? Prompts { get; init; }

    /// <summary>
    /// Gets or sets the resources capability.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpResourcesCapabilities? Resources { get; init; }

    /// <summary>
    /// Gets or sets the tools capability.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpToolsCapabilities? Tools { get; init; }

    /// <summary>
    /// Creates capabilities for a tools-only server.
    /// </summary>
    /// <param name="listChanged">Indicates whether the tools list can change.</param>
    /// <returns>MCP capabilities configured for tools only.</returns>
    public static McpCapabilities ForToolsOnly(bool listChanged) => new McpCapabilities
    {
        Tools = new McpToolsCapabilities(listChanged)
    };
}

/// <summary>
/// Represents MCP server information.
/// </summary>
public sealed class McpServerInfo
{
    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional server title.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    public string Version { get; init; } = string.Empty;
}

/// <summary>
/// Represents MCP completions capability.
/// </summary>
internal sealed record McpCompletionsCapabilities();

/// <summary>
/// Represents MCP logging capability.
/// </summary>
internal sealed record McpLoggingCapabilities();

/// <summary>
/// Represents MCP prompts capability.
/// </summary>
/// <param name="ListChanged">Indicates whether the prompts list can change.</param>
internal sealed record McpPromptsCapabilities(bool? ListChanged);

/// <summary>
/// Represents MCP resources capability.
/// </summary>
/// <param name="Subscribe">Indicates whether subscription to resources is supported.</param>
/// <param name="ListChanged">Indicates whether the resources list can change.</param>
internal sealed record McpResourcesCapabilities(bool? Subscribe, bool? ListChanged);

/// <summary>
/// Represents MCP tools capability.
/// </summary>
/// <param name="ListChanged">Indicates whether the tools list can change.</param>
internal sealed record McpToolsCapabilities(bool? ListChanged);
