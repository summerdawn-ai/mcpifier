using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpToolsListRpcHandler(IOptions<McpifyOptions> options) : IRpcHandler
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
