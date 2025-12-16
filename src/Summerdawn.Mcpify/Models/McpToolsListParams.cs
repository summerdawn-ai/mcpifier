namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents the parameters for an MCP tools/list request.
/// </summary>
internal sealed class McpToolsListParams
{
    /// <summary>
    /// Gets or sets the cursor for pagination.
    /// </summary>
    public string? Cursor { get; init; }
}
