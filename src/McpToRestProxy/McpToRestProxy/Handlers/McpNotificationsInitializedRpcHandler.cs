using Summerdawn.McpToRestProxy.Models;

namespace Summerdawn.McpToRestProxy.Handlers;

public sealed class McpNotificationsInitializedRpcHandler : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonRpcResponse.Empty);
    }
}
