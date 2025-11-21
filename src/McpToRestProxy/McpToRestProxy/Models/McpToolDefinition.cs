using System.Text.Json.Serialization;

namespace Summerdawn.McpToRestProxy.Models;

public class McpToolDefinition
{
    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    public string? Description { get; init; }

    public required InputSchema InputSchema { get; set; }
}

public class InputSchema
{
    public string Type { get; set; } = "object";

    public Dictionary<string, PropertySchema>? Properties { get; set; }

    // Spec says it's nullable but MCP Inspector throws on null
    public List<string> Required { get; set; } = [];
}

public class PropertySchema
{
    public string Type { get; set; } = "string";

    public string? Description { get; set; }
}
