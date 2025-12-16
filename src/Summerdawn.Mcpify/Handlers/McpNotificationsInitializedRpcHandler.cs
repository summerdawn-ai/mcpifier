using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Handlers;

/// <summary>
/// Handles the MCP notifications/initialized notification.
/// </summary>
public sealed class McpNotificationsInitializedRpcHandler(ILogger<McpNotificationsInitializedRpcHandler> logger) : IRpcHandler
{
    /// <inheritdoc/>
    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Handling notifications/initialized request with id {RequestId}", rpcRequest.Id);
        
        return Task.FromResult(JsonRpcResponse.Empty);
    }
}
