using System.Text.Json;

using Summerdawn.McpToRestProxy.Models;

namespace Summerdawn.McpToRestProxy.Handlers;

public interface IRpcHandler
{
    /// <summary>
    /// Handles a JSON-RPC request and returns a response.
    /// </summary>
    /// <exception cref="JsonException">Thrown when deserialization of request parameters fails.</exception>
    Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default);
}
