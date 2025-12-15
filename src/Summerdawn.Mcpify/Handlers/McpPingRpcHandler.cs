using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpPingRpcHandler(ILogger<McpPingRpcHandler> logger) : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Handling ping request with id {RequestId}", rpcRequest.Id);
        
        return Task.FromResult(JsonRpcResponse.Success(rpcRequest.Id));
    }
}
