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
    public static TheoryData<string, string, string, bool> GetEndToEndTestCases()
    {
        // Can reference the same test data as HTTP tests
        return HttpIntegrationTests.GetEndToEndTestCases();
    }

    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public async Task StdioServer_EndToEnd_AllScenarios(
        string testName,
        string mcpRequest,
        string expectedResponse,
        bool shouldVerifyMockCalled)
    {
        // Arrange
        var testStdio = new TestStdio();
        var mockResponses = new Dictionary<string, (HttpStatusCode, string)>
        {
            ["/api/test"] = (HttpStatusCode.OK, """{"status":"ok"}"""),
            ["/api/notfound"] = (HttpStatusCode.NotFound, """{"error":"not found"}"""),
            ["/api/servererror"] = (HttpStatusCode.InternalServerError, """{"error":"server error"}""")
        };

        var mockHandler = new MockHttpMessageHandler(mockResponses);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile("mappings.json", optional: false, reloadOnChange: false);
        builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));
        builder.Services.AddSingleton<IStdio>(testStdio);
        builder.Services.AddHttpClient<RestApiService>((sp, client) =>
        {
            client.BaseAddress = new Uri("http://example.com");
        })
        .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        var host = builder.Build();
        var stdioServer = host.Services.GetRequiredService<McpStdioServer>();
        stdioServer.Activate();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var hostTask = host.RunAsync(cts.Token);

        try
        {
            // Act
            await testStdio.WriteLineAsync(mcpRequest + "\n");
            string? actualResponse = await testStdio.ReadLineAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(actualResponse);

            var expected = JsonDocument.Parse(expectedResponse);
            var actual = JsonDocument.Parse(actualResponse);

            AssertJsonEquals(expected.RootElement, actual.RootElement, testName);

            if (shouldVerifyMockCalled)
            {
                Assert.True(mockHandler.WasCalled, $"Test '{testName}': Expected mock HTTP handler to be called");
            }
        }
        finally
        {
            await cts.CancelAsync();
            try { await hostTask; } catch (OperationCanceledException) { }
        }
    }

    private void AssertJsonEquals(JsonElement expected, JsonElement actual, string testName, string path = "$")
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in expected.EnumerateObject())
                {
                    Assert.True(actual.TryGetProperty(prop.Name, out var actualProp),
                        $"Test '{testName}': Missing property '{prop.Name}' at {path}");
                    AssertJsonEquals(prop.Value, actualProp, testName, $"{path}.{prop.Name}");
                }
                break;

            case JsonValueKind.Array:
                var expectedArray = expected.EnumerateArray().ToList();
                var actualArray = actual.EnumerateArray().ToList();
                Assert.Equal(expectedArray.Count, actualArray.Count);

                for (int i = 0; i < expectedArray.Count; i++)
                {
                    AssertJsonEquals(expectedArray[i], actualArray[i], testName, $"{path}[{i}]");
                }
                break;

            default:
                Assert.Equal(expected.ToString(), actual.ToString());
                break;
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
