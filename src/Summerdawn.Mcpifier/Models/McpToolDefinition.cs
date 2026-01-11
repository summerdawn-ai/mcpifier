using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents an MCP tool definition.
/// </summary>
public class McpToolDefinition
{
    private JsonElement inputSchema;
    private JsonSchema? inputSchemaObject;
    private readonly object lockObject = new();

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tool title.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the tool description.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the input schema for the tool.
    /// When set, automatically parses into a JsonSchema for validation.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public JsonElement InputSchema
    {
        get => inputSchema;
        set
        {
            lock (lockObject)
            {
                inputSchema = value;
                // Automatically parse when set
                if (value.ValueKind != JsonValueKind.Undefined && value.ValueKind != JsonValueKind.Null)
                {
                    inputSchemaObject = JsonSchema.FromText(value.GetRawText());
                }
            }
        }
    }

    /// <summary>
    /// Gets the deserialized JSON Schema for validation.
    /// </summary>
    public JsonSchema GetDeserializedInputSchema()
    {
        // Use double-checked locking pattern for thread-safe lazy initialization
        if (inputSchemaObject == null)
        {
            lock (lockObject)
            {
                if (inputSchemaObject == null)
                {
                    if (inputSchema.ValueKind == JsonValueKind.Undefined || inputSchema.ValueKind == JsonValueKind.Null)
                    {
                        // Return default object schema
                        inputSchemaObject = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
                    }
                    else
                    {
                        inputSchemaObject = JsonSchema.FromText(inputSchema.GetRawText());
                    }
                }
            }
        }
        return inputSchemaObject;
    }

    /// <summary>
    /// Efficiently sets both representations when both are already available.
    /// Used internally by converters and loaders.
    /// </summary>
    internal void SetInputSchema(JsonElement element, JsonSchema schema)
    {
        lock (lockObject)
        {
            inputSchema = element;
            inputSchemaObject = schema;
        }
    }
}
