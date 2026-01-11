using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Configuration;

/// <summary>
/// Minimal version of <see cref="McpifierOptions"/> for JSON (de-)serialization of mappings.json.
/// </summary>
internal class MinimalOptions
{
    [JsonPropertyName(nameof(Rest))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MinimalRestSection? Rest { get; set; }

    /// <summary>
    /// Gets or sets the list of tool definitions.
    /// </summary>
    [JsonPropertyName(nameof(Tools))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpifierToolMapping> Tools { get; set; } = [];
}

/// <summary>
/// Minimal version of <see cref="McpifierRestSection"/> for serialization.
/// </summary>
internal class MinimalRestSection
{
    /// <summary>
    /// Gets or sets the base address for REST API calls. Can be absolute or relative.
    /// </summary>
    [JsonPropertyName(nameof(BaseAddress))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseAddress { get; set; }
}

/// <summary>
/// Wrapper for <see cref="MinimalOptions"/> for serialization as "Mcpifier" root element.
/// </summary>
internal class MinimalOptionsWrapper
{
    [JsonPropertyName(nameof(Mcpifier))]
    public MinimalOptions Mcpifier { get; set; } = new();
}
