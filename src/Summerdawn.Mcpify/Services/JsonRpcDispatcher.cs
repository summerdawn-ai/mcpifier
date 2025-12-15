using System.Text.Json;

using Summerdawn.Mcpify.Handlers;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Services;

public class JsonRpcDispatcher(Func<string, IRpcHandler?> handlerFactory, ILogger<JsonRpcDispatcher> logger)
{
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
