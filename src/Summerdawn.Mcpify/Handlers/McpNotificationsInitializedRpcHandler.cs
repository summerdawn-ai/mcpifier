using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpNotificationsInitializedRpcHandler : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonRpcResponse.Empty);
    }
}
