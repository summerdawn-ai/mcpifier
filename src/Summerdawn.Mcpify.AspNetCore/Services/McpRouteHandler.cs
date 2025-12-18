using System.Text.Json;

using Microsoft.AspNetCore.Http.Extensions;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Services;

/// <summary>
/// Handles HTTP routing for Model Context Protocol calls and protected resource metadata.
/// </summary>
internal class McpRouteHandler(JsonRpcDispatcher dispatcher, IOptions<McpifyOptions> options, ILogger<McpRouteHandler> logger)
{
    /// <summary>
    /// Handles HTTP requests for MCP RPC calls at the configured route.
    /// </summary>
    public async Task HandleMcpRequestAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var traceId = context.TraceIdentifier;

        try
        {
            // If the request is not authenticated, return 401 Unauthorized and include a WWW-Authenticate header
            // as required by the specification.
            if (options.Value.Authentication.RequireAuthorization && !context.Request.Headers.Authorization.Any())
            {
                var url = new Uri(context.Request.GetEncodedUrl());

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer resource_metadata=\"{url.Scheme}://{url.Authority}/.well-known/oauth-protected-resource{url.AbsolutePath}\"";

                return;
            }

            // Otherwise dispatch the request to the dispatcher.
            JsonRpcRequest? rpcRequest;
            try
            {
                rpcRequest = await context.Request.ReadFromJsonAsync<JsonRpcRequest>();
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize MCP request as JSON-RPC.");

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(JsonRpcResponse.ParseError());

                return;
            }

            if (rpcRequest is null)
            {
                logger.LogWarning("Received null MCP request");

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(JsonRpcResponse.InvalidRequest(default));

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
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                await context.Response.WriteAsJsonAsync(rpcResponse);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status200OK;

                await context.Response.WriteAsJsonAsync(rpcResponse);
            }
        }
        finally
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

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
            var metadata = options.Value.Authentication.ResourceMetadata;

            context.Response.StatusCode = StatusCodes.Status200OK;

            await context.Response.WriteAsJsonAsync(metadata);
            
            logger.LogDebug("Served protected resource metadata for path {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error serving protected resource metadata");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}