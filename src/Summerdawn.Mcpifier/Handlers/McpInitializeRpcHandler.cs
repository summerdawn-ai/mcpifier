using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Handlers;

/// <summary>
/// Handles the MCP initialize request.
/// </summary>
public sealed class McpInitializeRpcHandler(IOptions<McpifierOptions> options, ILogger<McpInitializeRpcHandler> logger) : IRpcHandler
{
    /// <inheritdoc/>
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        // Deserialize, but ignore, params.
        rpcRequest.DeserializeParams<McpInitializeParams>();

        logger.LogDebug("Handling initialize request with id {RequestId}", rpcRequest.Id);

        var response = JsonRpcResponse.Success(rpcRequest.Id, BuildInitializeResult());
        return Task.FromResult(response);
    }

    private McpInitializeResult BuildInitializeResult() => new McpInitializeResult
    {
        ProtocolVersion = options.Value.ProtocolVersion,
        ServerInfo = new McpServerInfo
        {
            Name = options.Value.ServerInfo.Name,
            Title = options.Value.ServerInfo.Title,
            Version = options.Value.ServerInfo.Version,
        },
        Capabilities = McpCapabilities.ForToolsOnly(listChanged: false),
        Instructions = options.Value.Instructions
    };
}
