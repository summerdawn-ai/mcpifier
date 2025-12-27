using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Abstractions;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Background service that handles MCP JSON-RPC communication over stdio.
/// </summary>
public class McpStdioServer(IStdio stdio, IJsonRpcDispatcher dispatcher, ILogger<McpStdioServer> logger) : BackgroundService
{
    private readonly TaskCompletionSource<object?> activation = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Activates the stdio server to begin processing MCP requests.
    /// </summary>
    public void Activate()
    {
        activation.TrySetResult(null);
    }

    /// <summary>
    /// Executes the stdio server to process MCP requests.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that indicates when the service should stop.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Use BOM-less encoding
        var utf8Encoding = new UTF8Encoding(false);

        try
        {
            await activation.Task.WaitAsync(stoppingToken);

            await using var inputStream = stdio.GetStandardInput();
            await using var outputStream = stdio.GetStandardOutput();

            using var reader = new StreamReader(inputStream, utf8Encoding);
            await using var writer = new StreamWriter(outputStream, utf8Encoding);

            writer.AutoFlush = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                // MCP uses simple Line-Delimited JSON.
                string? requestPayload = await ReadLineAsync(reader, stoppingToken);

                // Break if the token fires or the console reader catches the Ctrl+C first
                if (requestPayload is null) break;

                if (string.IsNullOrWhiteSpace(requestPayload))
                {
                    // Treat blank/whitespace lines as InvalidRequest
                    string errorJson = JsonSerializer.Serialize<JsonRpcResponse>(JsonRpcResponse.InvalidRequest(), JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
                    await writer.WriteLineAsync(errorJson);
                    continue;
                }

                await HandleMcpRequestAsync(requestPayload, writer, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* Host shutdown */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in stdio server.");
        }
    }

    /// <summary>
    /// Handles an MCP request by deserializing the request payload, dispatching it, and writing the response.
    /// </summary>
    /// <param name="requestPayload">The request as Line-Delimited JSON.</param>
    /// <param name="writer">The <see cref="StreamWriter"/> used to write the response.</param>
    /// <param name="stoppingToken">A cancellation token that indicates when the service should stop.</param>
    protected internal async Task HandleMcpRequestAsync(string requestPayload, StreamWriter writer, CancellationToken stoppingToken)
    {
        var startTime = DateTime.UtcNow;
        string? rpcMethod = null;
        JsonElement requestId = default;

        try
        {
            JsonRpcRequest? rpcRequest;
            try
            {
                rpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(requestPayload, JsonRpcAndMcpJsonContext.Default.JsonRpcRequest);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize MCP request as JSON-RPC.");

                string errorJson = JsonSerializer.Serialize<JsonRpcResponse>(JsonRpcResponse.ParseError(), JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
                await writer.WriteLineAsync(errorJson);

                return;
            }

            if (rpcRequest is null)
            {
                logger.LogWarning("Received null MCP request");

                string errorJson = JsonSerializer.Serialize<JsonRpcResponse>(JsonRpcResponse.InvalidRequest(), JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
                await writer.WriteLineAsync(errorJson);

                return;
            }

            rpcMethod = rpcRequest.Method;
            requestId = rpcRequest.Id;

            logger.LogDebug("Processing MCP request: {RpcMethod} with id {RequestId}",
                rpcRequest.Method, rpcRequest.Id);

            var rpcResponse = await dispatcher.DispatchAsync(rpcRequest, stoppingToken);
            
            if (rpcResponse.IsEmpty())
            {
                // Don't send a response for notifications
                return;
            }

            string responseJson = JsonSerializer.Serialize<JsonRpcResponse>(rpcResponse, JsonRpcAndMcpJsonContext.Default.JsonRpcResponse);
            await writer.WriteLineAsync(responseJson);
        }
        finally
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            logger.LogInformation("MCP request: {Method} with id {RequestId} completed in {ElapsedMs}ms",
                rpcMethod ?? "unknown", requestId, elapsedMs);
        }
    }

    private static async Task<string?> ReadLineAsync(StreamReader reader, CancellationToken stoppingToken)
    {
        try
        {
            // ReadLineAsync doesn't properly cancel on stdin, so we check the token separately.
            var readTask = reader.ReadLineAsync(CancellationToken.None).AsTask();
            
            var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, stoppingToken));
                    
            if (completedTask != readTask)
            {
                // Cancellation was requested
                return null;
            }
                    
            return await readTask;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}