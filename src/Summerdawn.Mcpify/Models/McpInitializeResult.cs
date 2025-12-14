using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

public sealed class McpInitializeResult
{
    public string ProtocolVersion { get; init; } = string.Empty;

    public McpCapabilities Capabilities { get; init; } = new();

    public McpServerInfo ServerInfo { get; init; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; init; }
}

public sealed class McpCapabilities
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpCompletionsCapabilities? Completions { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpLoggingCapabilities? Logging { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpPromptsCapabilities? Prompts { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpResourcesCapabilities? Resources { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpToolsCapabilities? Tools { get; init; }

    public static McpCapabilities ForToolsOnly(bool listChanged) => new McpCapabilities
    {
        Tools = new McpToolsCapabilities(listChanged)
    };
}

public sealed class McpServerInfo
{
    public string Name { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    public string Version { get; init; } = string.Empty;
}

public sealed record McpCompletionsCapabilities();

public sealed record McpLoggingCapabilities();

public sealed record McpPromptsCapabilities(bool? ListChanged);

public sealed record McpResourcesCapabilities(bool? Subscribe, bool? ListChanged);

public sealed record McpToolsCapabilities(bool? ListChanged);
