using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpPingRpcHandler : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonRpcResponse.Success(rpcRequest.Id));
    }
}
