using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Summerdawn.Mcpifier.Abstractions;
using Summerdawn.Mcpifier.Services;

using Xunit.Abstractions;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Integration tests for stdio mode using in-memory streams.
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class StdioIntegrationTests(McpifierHostFactory factory, ITestOutputHelper output) : IClassFixture<McpifierHostFactory>
{
    private static readonly JsonSerializerOptions NormalizedJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    [Theory]
    [MemberData(nameof(GetDataForHandleRequests_Mcp))]
    public Task HandleRequest_WithMcpMethod_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(GetDataForHandleRequests_Http))]
    public Task HandleRequest_WithToolCall_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(GetDataForHandleRequests_Invalid))]
    public Task HandleRequest_WithInvalidRequest_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    private async Task HandleRequest_WithGivenRequest_ReturnsExpectedResponse(string scenario, string mcpRequest, string expectedResponse)
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

        var host = factory.WithApplicationBuilder(builder =>
        {
            // Mock stdio and HTTP API.
            builder.Services.AddSingleton<IStdio>(testStdio);
            builder.Services.AddHttpClient<RestApiService>((sp, client) =>
                {
                    client.BaseAddress = new Uri("http://example.com");
                })
                .ConfigurePrimaryHttpMessageHandler(() => mockHandler);
        });

        var stdioServer = host.Services.GetRequiredService<McpStdioServer>();
        stdioServer.Activate();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var hostTask = host.RunAsync(cts.Token);

        try
        {
            // Act
            await testStdio.WriteLineAsync(mcpRequest + "\n");
            string? actualResponse = await testStdio.ReadLineAsync(TimeSpan.FromSeconds(5));

            actualResponse = NormalizeJson(actualResponse);

            output.WriteLine("Scenario:          {0}", scenario);
            output.WriteLine("Request:           {0}", mcpRequest);
            output.WriteLine("Expected response: {0}", expectedResponse);
            output.WriteLine("Actual response:   {0}", actualResponse);

            // Assert
            Assert.NotNull(actualResponse);
            Assert.Equal(expectedResponse, actualResponse);
        }
        finally
        {
            await cts.CancelAsync();
            try { await hostTask; } catch (OperationCanceledException) { }
        }
    }

    public static TheoryData<string, string, string> GetDataForHandleRequests_Mcp() => new()
    {
        // Format: (scenario, mcpRequest, expectedResponse)
        {
            "Ping",
            """{"jsonrpc":"2.0","id":1,"method":"ping"}""",
            """{"jsonrpc":"2.0","id":1,"result":{}}"""
        },
        {
            "Initialize",
            """{"jsonrpc":"2.0","id":2,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""",
            """{"jsonrpc":"2.0","id":2,"result":{"protocolVersion":"2025-06-18","capabilities":{"tools":{"listChanged":false}},"serverInfo":{"name":"mcpifier","version":"1.0"}}}"""
        },
        {
            "ToolsList",
            """{"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}""",
            """{"jsonrpc":"2.0","id":3,"result":{"tools":[{"name":"test_tool","description":"test tool","inputSchema":{"type":"object","required":[]}},{"name":"test_tool_404","description":"Tool that returns 404","inputSchema":{"type":"object","required":[]}},{"name":"test_tool_500","description":"Tool that returns 500","inputSchema":{"type":"object","required":[]}}]}}"""
        }
    };

    public static TheoryData<string, string, string> GetDataForHandleRequests_Http() => new()
    {
        // Format: (scenario, mcpRequest, expectedResponse)
        {
            "Success",
            """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"test_tool","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":4,"result":{"content":[{"type":"text","text":"{\"status\":\"ok\"}"}],"structuredContent":{"status":"ok"},"isError":false}}"""
        },
        {
            "404",
            """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"test_tool_404","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":5,"result":{"content":[{"type":"text","text":"REST API returned error code 404: '{\"error\":\"not found\"}'"}],"isError":true}}"""
        },
        {
            "500",
            """{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"test_tool_500","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":6,"result":{"content":[{"type":"text","text":"REST API returned error code 500: '{\"error\":\"server error\"}'"}],"isError":true}}"""
        }
    };

    public static TheoryData<string, string, string> GetDataForHandleRequests_Invalid() => new()
    {
        // Format: (scenario, mcpRequest, expectedResponse)
        {
            "EmptyJson",
            """ """,
            """{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"Parse error"}}"""
        },
        {
            "WrongJson_NotObject",
            """null""",
            """{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"Parse error"}}"""
        },
        {
            "MalformedJson",
            """"{"jsonrpc":"2.0","id":1,"method":"ping"""",
            """{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"Parse error"}}"""
        },
        {
            "MissingJsonRpc",
            """{"id":1,"method":"ping"}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}"""
        },
        {
            "InvalidMethod_NotString",
            """{"jsonrpc":"2.0","id":1,"method":123}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}"""
        },
        {
            "MissingMethod",
            """{"jsonrpc":"2.0","id":1}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}"""
        },
        {
            "MethodNotFound",
            """{"jsonrpc":"2.0","id":1,"method":"unknown_method","params":{}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"Method not found"}}"""
        },
        {
            "InvalidParams_NotObject",
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":"invalid"}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        },
        {
            "ToolCall_NonExistent",
            """{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"non_existent_tool","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":7,"error":{"code":-32602,"message":"Invalid params"}}"""
        },
        {
            "ToolCall_MissingArguments",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"arguments":{}}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        },
        {
            "ToolCall_InvalidArguments_NotObject",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"test_tool","arguments":"invalid"}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        }
    };

    private static string? NormalizeJson(string? json)
    {
        if (json is null) { return null; }

        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, NormalizedJsonOptions);
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
