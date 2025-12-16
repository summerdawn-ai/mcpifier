using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents the result of an MCP tools/call request.
/// </summary>
internal class McpToolsCallResult
{
    /// <summary>
    /// Gets or sets the content of the tool result.
    /// </summary>
    public List<object> Content { get; init; } = [];

    /// <summary>
    /// Gets or sets the structured content of the tool result as JSON.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? StructuredContent { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the tool execution resulted in an error.
    /// </summary>
    public bool IsError { get; set; } = false;
}

/// <summary>
/// Represents text content in an MCP response.
/// </summary>
internal record McpTextContent
{
    /// <summary>
    /// Gets the content type. Always "text".
    /// </summary>
    public string Type => "text";

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string? Text { get; init; }
}
