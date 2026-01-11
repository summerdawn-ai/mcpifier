using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents an MCP tool definition.
/// </summary>
public class McpToolDefinition
{
    private static readonly JsonSchema DefaultSchema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

    private readonly object schemaSync = new();

    private JsonElement inputSchema;
    private Lazy<JsonSchema> deserializedInputSchema = new(DefaultSchema);

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
    /// </summary>
    /// <remarks>
    /// When set, automatically creates a lazy initializer for the parsed
    /// schema returned by <see cref="GetDeserializedInputSchema"/>.
    /// </remarks>
    [JsonPropertyName("inputSchema")]
    public JsonElement InputSchema
    {
        get => inputSchema;
        set
        {
            lock (schemaSync)
            {
                inputSchema = value;

                deserializedInputSchema = value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                    ? new Lazy<JsonSchema>(DefaultSchema)
                    : new Lazy<JsonSchema>(() => JsonSchema.FromText(value.GetRawText()), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }

    /// <summary>
    /// Gets the deserialized JSON Schema for validation.
    /// </summary>
    public JsonSchema GetDeserializedInputSchema() => deserializedInputSchema.Value;

    /// <summary>
    /// Efficiently sets both representations when both are already available.
    /// Used internally by converters and loaders.
    /// </summary>
    internal void SetInputSchema(JsonElement element, JsonSchema schema)
    {
        lock (schemaSync)
        {
            inputSchema = element;

            // Use an immediate lazy initializer since we already have the schema.
            deserializedInputSchema = new Lazy<JsonSchema>(schema);
        }
    }
}