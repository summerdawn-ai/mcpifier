using System.Text.Json;

using Summerdawn.Mcpifier.Handlers;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Dispatches JSON-RPC requests to appropriate handlers.
/// </summary>
public class JsonRpcDispatcher(Func<string, IRpcHandler?> handlerFactory, ILogger<JsonRpcDispatcher> logger) : IJsonRpcDispatcher
{
    /// <summary>
    /// Dispatches a JSON-RPC request to the appropriate handler.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A JSON-RPC response.</returns>
    public async Task<JsonRpcResponse> DispatchAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        if (!rpcRequest.IsValidVersion())
        {
            logger.LogWarning("Invalid JSON-RPC version received: {Version}", rpcRequest.Version);
         
            return JsonRpcResponse.InvalidRequest(rpcRequest.Id);
        }

        var handler = handlerFactory.Invoke(rpcRequest.Method.ToLowerInvariant());
        if (handler is null)
        {
            logger.LogWarning("JSON-RPC method unknown or not supported: {Method}", rpcRequest.Method);

            return JsonRpcResponse.MethodNotFound(rpcRequest.Id, rpcRequest.Method);
        }

        try
        {
            logger.LogDebug("Responding to JSON-RPC {Method} with id {RequestId}", rpcRequest.Method, rpcRequest.Id);

            return await handler.HandleAsync(rpcRequest, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid request parameters for JSON-RPC method {Method}", rpcRequest.Method);
            return JsonRpcResponse.InvalidParams(rpcRequest.Id, $"Invalid params: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling JSON-RPC method {Method}", rpcRequest.Method);
            return JsonRpcResponse.InternalError(rpcRequest.Id, $"Internal error: {ex.Message}");
        }
    }   
}
