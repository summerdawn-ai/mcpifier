using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Services;

/// <summary>
/// Dispatches JSON-RPC requests to appropriate handlers.
/// </summary>
public interface IJsonRpcDispatcher
{
    /// <summary>
    /// Dispatches a JSON-RPC request to the appropriate handler.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A JSON-RPC response.</returns>
    Task<JsonRpcResponse> DispatchAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken);
}
