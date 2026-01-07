namespace Summerdawn.Mcpifier.Models;

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
