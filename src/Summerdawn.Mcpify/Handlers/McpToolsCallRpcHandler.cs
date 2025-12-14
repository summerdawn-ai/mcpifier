using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.Handlers;

public sealed class McpToolsCallRpcHandler(RestProxyService proxyService, IHttpContextAccessor httpContextAccessor, IOptions<McpifyOptions> options, ILogger<McpToolsCallRpcHandler> logger) : IRpcHandler
{
    public async Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.DeserializeRequiredParams<McpToolsCallParams>();

        var tool = options.Value.Tools.FirstOrDefault(t => t.Mcp.Name == parameters.Name);
        if (tool is null)
        {
            logger.LogWarning("Tool not found: {ToolName}", parameters.Name);

            return JsonRpcResponse.ErrorResponse(request.Id, 404, $"Tool '{parameters.Name}' not found");
        }

        var (isValid, errorMessage) = ToolValidator.ValidateArguments(tool.Mcp, parameters.Arguments);
        if (!isValid)
        {
            var message = errorMessage ?? "Invalid arguments";
            
            logger.LogWarning("Invalid arguments for tool {ToolName}: {Error}", parameters.Name, message);

            return JsonRpcResponse.ErrorResponse(request.Id, 400, message);
        }

        // Get auth header from HTTP context, if there is an HTTP context.
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

        var (success, statusCode, responseBody) = await proxyService.ExecuteToolAsync(tool, parameters.Arguments, authHeader);
        if (!success)
        {
            logger.LogWarning("REST API returned error {StatusCode} for tool {ToolName}", statusCode, parameters.Name);

            // According to the spec, tool failures are returned as normal tool
            // results with IsError: true, not as JSON-RPC error responses.
            var errorResult = new McpToolsCallResult
            {
                Content = CreateErrorContent(statusCode, responseBody),

                IsError = true
            };

            return JsonRpcResponse.Success(request.Id, errorResult);
        }

        var result = new McpToolsCallResult
        {
            Content = CreateContent(responseBody),
            StructuredContent = CreateStructuredContent(responseBody),

            IsError = false
        };

        return JsonRpcResponse.Success(request.Id, result);
    }

    private static List<object> CreateContent(string responseBody) =>
    [
            new McpTextContent { Text = responseBody }
    ];

    private static List<object> CreateErrorContent(int statusCode, string responseBody) =>
    [
        new McpTextContent { Text = $"REST API returned error code {statusCode}: '{responseBody}'" }
    ];

    private static JsonElement? CreateStructuredContent(string responseBody)
    {
        JsonElement responseAsJson;
        try
        {
            responseAsJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
        }
        catch (JsonException)
        {
            // Ignore response if not JSON.
            return null;
        }

        return responseAsJson.ValueKind switch
        {
            JsonValueKind.Object => responseAsJson,
            // StructuredContent must be an object, not an array.
            JsonValueKind.Array => JsonSerializer.Deserialize<JsonElement>($$"""{ "results": {{responseBody}} }"""),
            _ => null,
        };
    }
}
