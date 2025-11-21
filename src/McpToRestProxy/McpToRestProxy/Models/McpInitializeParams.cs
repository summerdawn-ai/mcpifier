namespace Summerdawn.McpToRestProxy.Models;

public sealed class McpInitializeParams
{
    public string? ProtocolVersion { get; init; }

    public McpClientInfo? ClientInfo { get; init; }
}

public sealed record McpClientInfo(string? Name, string? Version);
