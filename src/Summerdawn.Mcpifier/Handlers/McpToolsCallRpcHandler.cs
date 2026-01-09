using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Handlers;

/// <summary>
/// Handles the MCP tools/call request.
/// </summary>
public sealed class McpToolsCallRpcHandler(RestApiService restApiService, IOptions<McpifierOptions> options, ILogger<McpToolsCallRpcHandler> logger, IHttpContextAccessor? httpContextAccessor = null) : IRpcHandler
{
    /// <inheritdoc/>
    public async Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = request.DeserializeRequiredParams<McpToolsCallParams>();

        var tool = options.Value.Tools.FirstOrDefault(t => t.Mcp.Name == parameters.Name);
        if (tool is null)
        {
            logger.LogWarning("Tool not found: {ToolName}", parameters.Name);

            return JsonRpcResponse.InvalidParams(request.Id, $"Tool '{parameters.Name}' not found");
        }

        var (isValid, errorMessage) = ToolValidator.ValidateArguments(tool.Mcp, parameters.Arguments);
        if (!isValid)
        {
            errorMessage ??= "Invalid arguments";

            logger.LogWarning("Invalid arguments for tool {ToolName}: {Error}", parameters.Name, errorMessage);

            return JsonRpcResponse.InvalidParams(request.Id, errorMessage);
        }

        var forwardedHeaders = GetForwardedHeaders(httpContextAccessor?.HttpContext?.Request);

        var (success, statusCode, responseBody) = await restApiService.ExecuteToolAsync(tool, parameters.Arguments, forwardedHeaders);
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

    private Dictionary<string, string> GetForwardedHeaders(HttpRequest? request)
    {
        var forwardedHeaderNames = options.Value.Rest.ForwardedHeaders.Where(h => h.Value == true).Select(h => h.Key).ToList();
        var requestHeaders = request?.Headers;

        if (forwardedHeaderNames.Any() && requestHeaders is null)
        {
            logger.LogWarning("Header forwarding is configured for {count} headers, but no HTTP context is available.", forwardedHeaderNames.Count);
        }

        if (requestHeaders is null)
        {
            return [];
        }

        var forwardedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string headerName in forwardedHeaderNames)
        {
            string? headerValue = requestHeaders[headerName].FirstOrDefault();

            if (!string.IsNullOrEmpty(headerValue))
            {
                forwardedHeaders[headerName] = headerValue;
            }
        }

        return forwardedHeaders;
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
            responseAsJson = JsonDocument.Parse(responseBody).RootElement.Clone();
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
            JsonValueKind.Array => JsonDocument.Parse($$"""{ "results": {{responseBody}} }""").RootElement.Clone(),
            _ => null,
        };
    }
}
