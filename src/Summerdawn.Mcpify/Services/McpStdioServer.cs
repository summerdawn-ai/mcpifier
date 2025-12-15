using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpify.Abstractions;
using Summerdawn.Mcpify.Models;

namespace Summerdawn.Mcpify.Services;

/// <summary>
/// Background service that handles MCP JSON-RPC communication over stdio.
/// </summary>
public sealed class McpStdioServer(IStdio stdio, JsonRpcDispatcher dispatcher, ILogger<McpStdioServer> logger) : BackgroundService
{
    /// <summary>
    /// Defines JSON serialization options for stdio communication.
    /// </summary>
    private static readonly JsonSerializerOptions StdioJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

                if (string.IsNullOrWhiteSpace(requestPayload))
                {
                    continue;
                }

                JsonRpcRequest? rpcRequest;
                try
                {
                    rpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(requestPayload, StdioJsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize JSON-RPC request.");
                    continue;
                }

                if (rpcRequest is null) continue;

                var rpcResponse = await dispatcher.DispatchAsync(rpcRequest, stoppingToken);
                if (rpcResponse.IsEmpty()) continue;
               
                string responseJson = JsonSerializer.Serialize(rpcResponse, StdioJsonOptions);

                await writer.WriteLineAsync(responseJson);
            }
        }
        catch (OperationCanceledException) { /* Host shutdown */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in stdio server.");
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