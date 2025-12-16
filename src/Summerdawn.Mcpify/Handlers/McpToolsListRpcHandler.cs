using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpToolsListRpcHandler(IOptions<McpifyOptions> options, ILogger<McpToolsListRpcHandler> logger) : IRpcHandler
{
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        // Deserialize, but ignore, params.
        rpcRequest.DeserializeParams<McpToolsListParams>();

        logger.LogDebug("Handling tools/list request with id {RequestId}", rpcRequest.Id);

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
