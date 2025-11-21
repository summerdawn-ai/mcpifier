using Summerdawn.McpToRestProxy.Configuration;
using Summerdawn.McpToRestProxy.Models;

namespace Summerdawn.McpToRestProxy.Handlers;

public sealed class McpToolsListRpcHandler(IOptions<ProxyOptions> options) : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        // Deserialize, but ignore, params.
        rpcRequest.DeserializeParams<McpToolsListParams>();

        var mcpTools = options.Value.Tools
            .Select(t => t.Mcp)
            .ToList();

        var result = new McpToolsListResult
        {
            Tools = mcpTools
        };

        return Task.FromResult(JsonRpcResponse.Success(rpcRequest.Id, result));
    }
}
