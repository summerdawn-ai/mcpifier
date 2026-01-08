using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents an MCP tool definition.
/// </summary>
public class McpToolDefinition
{
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
    /// Internal storage as JsonSchema for validation.
    /// </summary>
    [JsonIgnore]
    public JsonSchema InputSchemaObject { get; set; } = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

    /// <summary>
    /// Input schema as JsonElement for JSON serialization (MCP protocol).
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public JsonElement InputSchema { get; set; }
}
