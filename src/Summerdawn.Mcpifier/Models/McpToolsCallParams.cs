using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents the parameters for an MCP tools/call request.
/// </summary>
internal sealed class McpToolsCallParams
{
    /// <summary>
    /// Gets or sets the name of the tool to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement> Arguments { get; init; } = [];
}
