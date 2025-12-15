using Microsoft.AspNetCore.Http.Extensions;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Services;

/// <summary>
/// Handles HTTP routing for Model Context Protocol calls and protected resource metadata.
/// </summary>
internal class McpRouteHandler(JsonRpcDispatcher dispatcher, IOptions<McpifyOptions> options)
{
    /// <summary>
    /// Handles HTTP requests for MCP RPC calls at the configured route.
    /// </summary>
    public async Task HandleMcpRequestAsync(HttpContext context)
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
        var rpcRequest = await context.Request.ReadFromJsonAsync<JsonRpcRequest>();

        var rpcResponse = await dispatcher.DispatchAsync(rpcRequest!, CancellationToken.None);

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

    /// <summary>
    /// Handles HTTP requests for the protected resource metadata endpoint '/.well-known/oauth-protected-resource/{route}'.
    /// </summary>
    public async Task HandleProtectedResourceAsync(HttpContext context)
    {
        var metadata = options.Value.Authentication.ResourceMetadata;

        context.Response.StatusCode = StatusCodes.Status200OK;

        await context.Response.WriteAsJsonAsync(metadata);
    }
}