using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents the parameters for an MCP tools/call request.
/// </summary>
// The constructor is needed because the JSON source generator
// ignores the default values of init-only properties.
internal sealed class McpToolsCallParams(string? name = null, Dictionary<string, JsonElement>? arguments = null)
{
    /// <summary>
    /// Gets or sets the name of the tool to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = name ?? string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement> Arguments { get; init; } = arguments ?? [];
}
