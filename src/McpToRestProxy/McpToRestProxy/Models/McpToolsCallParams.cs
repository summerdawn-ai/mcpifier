using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.McpToRestProxy.Models;

public sealed class McpToolsCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement> Arguments { get; init; } = [];
}
