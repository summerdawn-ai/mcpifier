using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Configuration;

public class ProxyToolDefinition
{
    public required McpToolDefinition Mcp { get; set; }

    public required RestConfiguration Rest { get; set; }
}

public class RestConfiguration
{
    public string Method { get; set; } = "GET";

    public string Path { get; set; } = string.Empty;

    public string? Query { get; set; }

    public string? Body { get; set; }
}
