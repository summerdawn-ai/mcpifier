using Summerdawn.McpToRestProxy.Models;

namespace Summerdawn.McpToRestProxy.Configuration;

public class ProxyOptions
{
    public string ApiBaseUrl { get; set; } = "/";

    public Dictionary<string, string> ApiDefaultHeaders { get; set; } = [];

    public string ProtocolVersion { get; set; } = "2025-06-18";

    public McpServerInfo ServerInfo { get; set; } = new();

    public string? Instructions { get; set; }

    public List<ProxyToolDefinition> Tools { get; set; } = [];

    public ProxyAuthenticationSection Authentication { get; set; } = new();
}

public class ProxyAuthenticationSection
{
    public bool RequireAuthorization { get; set; } = false;

    public ProtectedResourceMetadata? ResourceMetadata { get; set; }
}