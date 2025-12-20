using System.Text.Json;

using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Handlers;

/// <summary>
/// Defines a handler for JSON-RPC requests in the MCP protocol.
/// </summary>
public interface IRpcHandler
{
    /// <summary>
    /// Handles a JSON-RPC request and returns a response.
    /// </summary>
    /// <exception cref="JsonException">Thrown when deserialization of request parameters fails.</exception>
    Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default);
}
