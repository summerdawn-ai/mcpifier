using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Summerdawn.Mcpifier.Abstractions;
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Integration tests for stdio mode using in-memory streams.
/// </summary>
public class StdioIntegrationTests
{
    [Fact]
    public async Task StdioServer_WithInMemoryStreams_ProcessesJsonRpcRequest()
    {
        // Arrange
        var testStdio = new TestStdio();
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}")
            });
        });

        var builder = Host.CreateApplicationBuilder();

        // Load test mappings
        builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: false);

        // Configure Mcpifier with stdio mode
        builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

        // Replace IStdio with TestStdio
        builder.Services.AddSingleton<IStdio>(testStdio);

        // Replace RestApiService HttpClient with mock handler
        builder.Services.AddHttpClient<RestApiService>((sp, client) =>
        {
            client.BaseAddress = new Uri("http://example.com");
        })
        .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        // Redirect console logging to stderr
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        var host = builder.Build();

        // Get the McpStdioServer and activate it
        var stdioServer = host.Services.GetRequiredService<McpStdioServer>();
        stdioServer.Activate();

        // Start the host in the background
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var hostTask = host.RunAsync(cts.Token);

        try
        {
            // Act - Write a JSON-RPC request
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list",
                @params = new { }
            };

            string requestJson = JsonSerializer.Serialize(request) + "\n";
            await testStdio.WriteLineAsync(requestJson);

            // Read the response with timeout
            string? responseJson = await testStdio.ReadLineAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(responseJson);
            Assert.NotEmpty(responseJson);

            var jsonDoc = JsonDocument.Parse(responseJson);
            Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
            Assert.True(result.TryGetProperty("tools", out var tools));

            var toolsArray = tools.EnumerateArray().ToArray();

            Assert.Equal(1, toolsArray.Length);
            Assert.Equal("test_tool", toolsArray[0].GetProperty("name").GetString());
        }
        finally
        {
            // Cleanup
            await cts.CancelAsync();
            try
            {
                await hostTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling the host
            }
        }
    }
}

/// <summary>
/// Test implementation of IStdio using System.IO.Pipelines for connected streams.
/// </summary>
public class TestStdio : IStdio
{
    private readonly Pipe inputPipe = new Pipe();
    private readonly Pipe outputPipe = new Pipe();
    private readonly Stream inputStream;
    private readonly Stream outputStream;

    public TestStdio()
    {
        // Create streams from pipes
        // Server reads from inputPipe.Reader (test writes to inputPipe.Writer)
        inputStream = inputPipe.Reader.AsStream();

        // Server writes to outputPipe.Writer (test reads from outputPipe.Reader)
        outputStream = outputPipe.Writer.AsStream();
    }

    public Stream GetStandardInput() => inputStream;
    public Stream GetStandardOutput() => outputStream;

    /// <summary>
    /// Write a line to the input stream (simulating stdin for the server).
    /// </summary>
    public async Task WriteLineAsync(string line)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(line);
        await inputPipe.Writer.WriteAsync(bytes);
        await inputPipe.Writer.FlushAsync();
    }

    /// <summary>
    /// Read a line from the output stream (reading stdout from the server).
    /// </summary>
    public async Task<string?> ReadLineAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        var reader = new StreamReader(outputPipe.Reader.AsStream(), Encoding.UTF8);
        var readTask = reader.ReadLineAsync(cts.Token);

        try
        {
            return await readTask;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Timeout reading from output stream after {timeout.TotalSeconds} seconds");
        }
    }
}
