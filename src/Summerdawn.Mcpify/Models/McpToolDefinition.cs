using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents an MCP tool definition.
/// </summary>
public class McpToolDefinition
{
    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the optional tool title.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the tool description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the input schema for the tool.
    /// </summary>
    public required InputSchema InputSchema { get; set; }
}

/// <summary>
/// Represents a JSON Schema for tool input validation.
/// </summary>
public class InputSchema
{
    /// <summary>
    /// Gets or sets the schema type. Typically "object".
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// Gets or sets the properties of the schema.
    /// </summary>
    public Dictionary<string, PropertySchema>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the list of required property names.
    /// </summary>
    public List<string> Required { get; set; } = [];
}

/// <summary>
/// Represents a property in a JSON Schema.
/// </summary>
public class PropertySchema
{
    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Gets or sets the property description.
    /// </summary>
    public string? Description { get; set; }
}
