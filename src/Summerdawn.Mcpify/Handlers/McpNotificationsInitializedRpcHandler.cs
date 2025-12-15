using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpNotificationsInitializedRpcHandler(ILogger<McpNotificationsInitializedRpcHandler> logger) : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Handling notifications/initialized request with id {RequestId}", rpcRequest.Id);
        
        return Task.FromResult(JsonRpcResponse.Empty);
    }
}
