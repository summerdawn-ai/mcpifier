using System.Text.Json;

using Microsoft.AspNetCore.Http.Extensions;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Handles HTTP routing for Model Context Protocol calls and protected resource metadata.
/// </summary>
public class McpRouteHandler(IJsonRpcDispatcher dispatcher, IOptions<McpifierOptions> options, ILogger<McpRouteHandler> logger)
{
    /// <summary>
    /// Handles HTTP requests for MCP RPC calls at the configured route.
    /// </summary>
    public async Task HandleMcpRequestAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        string method = context.Request.Method;
        var path = context.Request.Path;
        string traceId = context.TraceIdentifier;

        try
        {
            // If the request is not authenticated, return 401 Unauthorized and include a WWW-Authenticate header
            // as required by the specification.
            if (options.Value.Authorization.RequireAuthorization && !context.Request.Headers.Authorization.Any())
            {
                var url = new Uri(context.Request.GetEncodedUrl());

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer resource_metadata=\"{url.Scheme}://{url.Authority}/.well-known/oauth-protected-resource{url.AbsolutePath}\"";

                return;
            }

            // Otherwise dispatch the request to the dispatcher.
            JsonElement rpcRequestId = default;
            JsonRpcRequest? rpcRequest;
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                string requestPayload = await reader.ReadToEndAsync();

                // Try to parse as JsonDocument first to extract id if possible.
                using var requestDocument = JsonDocument.Parse(requestPayload);
                if (requestDocument.RootElement.TryGetProperty("id", out var idProperty))
                {
                    rpcRequestId = idProperty.Clone();
                }

                // Then deserialize to request object.
                rpcRequest = requestDocument.Deserialize<JsonRpcRequest>(JsonRpcAndMcpJsonContext.Default.JsonRpcRequest);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize MCP request as JSON-RPC.");

                // If we could parse JSON but deserialization failed (e.g., wrong types), it's InvalidRequest
                // If we couldn't even parse JSON, it's ParseError
                var errorResponse = rpcRequestId.ValueKind != JsonValueKind.Undefined
                    ? JsonRpcResponse.InvalidRequest(rpcRequestId)
                    : JsonRpcResponse.ParseError();

                context.Response.StatusCode = StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync<JsonRpcResponse>(errorResponse, JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);

                return;
            }

            if (rpcRequest is null)
            {
                logger.LogWarning("Received null MCP request");

                context.Response.StatusCode = StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync<JsonRpcResponse>(JsonRpcResponse.ParseError(), JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);

                return;
            }

            logger.LogDebug("Processing MCP request: {RpcMethod} with id {RequestId} [TraceId: {TraceId}]",
                rpcRequest.Method, rpcRequest.Id, traceId);

            var rpcResponse = await dispatcher.DispatchAsync(rpcRequest, CancellationToken.None);

            if (rpcResponse.IsEmpty())
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            }
            else if (rpcResponse.IsError())
            {
                context.Response.StatusCode = StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync<JsonRpcResponse>(rpcResponse, JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync<JsonRpcResponse>(rpcResponse, JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
            }
        }
        finally
        {
            double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            logger.LogInformation("MCP request: {Method} {Path} -> {StatusCode} in {ElapsedMs}ms [TraceId: {TraceId}]",
                method, (string)path, context.Response.StatusCode, elapsedMs, traceId);
        }
    }

    /// <summary>
    /// Handles HTTP requests for the protected resource metadata endpoint '/.well-known/oauth-protected-resource/{route}'.
    /// </summary>
    public async Task HandleProtectedResourceAsync(HttpContext context)
    {
        try
        {
            var metadata = options.Value.Authorization.ResourceMetadata;

            context.Response.StatusCode = StatusCodes.Status200OK;

            await context.Response.WriteAsJsonAsync<ProtectedResourceMetadata?>(metadata, JsonRpcAndMcpJsonContext.Default.ProtectedResourceMetadata!);

            logger.LogDebug("Served protected resource metadata for path {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error serving protected resource metadata");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
