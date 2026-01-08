using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Legacy helper class for building JSON Schema from OpenAPI (internal use only).
/// Kept for backward compatibility with tests.
/// </summary>
[Obsolete("This class is for internal use only and will be removed in a future version. Use JsonSchema directly.")]
public class InputSchema
{
    /// <summary>
    /// Gets or sets the schema type. Typically "object".
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// Gets or sets the properties of the schema.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, PropertySchema>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the list of required property names.
    /// </summary>
    public List<string> Required { get; set; } = [];
}

/// <summary>
/// Legacy helper class for representing property schemas (internal use only).
/// Kept for backward compatibility with tests.
/// </summary>
[Obsolete("This class is for internal use only and will be removed in a future version. Use JsonSchema directly.")]
public class PropertySchema
{
    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Gets or sets the property description.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the property format (e.g., date-time, int32).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the enum values for the property.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object>? Enum { get; set; }

    /// <summary>
    /// Gets or sets the items schema for array types.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PropertySchema? Items { get; set; }

    /// <summary>
    /// Gets or sets nested properties for object types.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, PropertySchema>? Properties { get; set; }
}
