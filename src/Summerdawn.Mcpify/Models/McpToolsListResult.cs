using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

public sealed class McpToolsListResult
{
    public IReadOnlyList<McpToolDefinition> Tools { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; init; }
}
