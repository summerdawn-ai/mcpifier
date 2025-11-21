using Summerdawn.McpToRestProxy.Models;

namespace Summerdawn.McpToRestProxy.Handlers;

public sealed class McpPingRpcHandler : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonRpcResponse.Success(rpcRequest.Id));
    }
}
