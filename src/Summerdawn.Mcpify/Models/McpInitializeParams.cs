namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents the parameters for an MCP initialize request.
/// </summary>
public sealed class McpInitializeParams
{
    /// <summary>
    /// Gets or sets the protocol version requested by the client.
    /// </summary>
    public string? ProtocolVersion { get; init; }

    /// <summary>
    /// Gets or sets the client information.
    /// </summary>
    public McpClientInfo? ClientInfo { get; init; }
}

/// <summary>
/// Represents MCP client information.
/// </summary>
/// <param name="Name">The client name.</param>
/// <param name="Version">The client version.</param>
public sealed record McpClientInfo(string? Name, string? Version);
