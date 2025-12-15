using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Configuration;

public class McpifyOptions
{
    public McpifyRestSection Rest { get; set; } = new();

    public string ProtocolVersion { get; set; } = "2025-06-18";

    public McpServerInfo ServerInfo { get; set; } = new();

    public string? Instructions { get; set; }

    public List<ProxyToolDefinition> Tools { get; set; } = [];

    public McpifyAuthenticationSection Authentication { get; set; } = new();
}

public class McpifyRestSection
{
    public string BaseAddress { get; set; } = "/";

    public Dictionary<string, string> DefaultHeaders { get; set; } = [];

    public Dictionary<string, bool> ForwardedHeaders { get; set; } = [];
}

public class McpifyAuthenticationSection
{
    public bool RequireAuthorization { get; set; } = false;

    public ProtectedResourceMetadata? ResourceMetadata { get; set; }
}