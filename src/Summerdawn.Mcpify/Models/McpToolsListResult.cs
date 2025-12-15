using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents the result of an MCP tools/list request.
/// </summary>
public sealed class McpToolsListResult
{
    /// <summary>
    /// Gets or sets the list of available tools.
    /// </summary>
    public IReadOnlyList<McpToolDefinition> Tools { get; init; } = [];

    /// <summary>
    /// Gets or sets the cursor for pagination.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; init; }
}
