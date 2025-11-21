using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.McpToRestProxy.Models;

public class McpToolsCallResult
{
    // Has to be object, not interface, so that it's serialized with all properties
    public List<object> Content { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? StructuredContent { get; init; }

    public bool IsError { get; set; } = false;
}

public record McpTextContent
{
    public string Type => "text";

    public string? Text { get; init; }
}
