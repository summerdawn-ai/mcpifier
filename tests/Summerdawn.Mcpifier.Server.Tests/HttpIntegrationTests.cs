using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Integration tests for HTTP mode using WebApplicationFactory.
/// </summary>
public class HttpIntegrationTests(McpifierServerFactory factory) : IClassFixture<McpifierServerFactory>
{
    public static TheoryData<string, string, string, bool> GetEndToEndTestCases()
    {
        var data = new TheoryData<string, string, string, bool>();
        // Format: (testName, mcpRequest, expectedResponse, shouldVerifyMockCalled)

        // === Protocol Flow Tests ===

        data.Add(
            "Ping",
            """{"jsonrpc":"2.0","id":1,"method":"ping"}""",
            """{"jsonrpc":"2.0","id":1,"result":{}}""",
            false
        );

        data.Add(
            "Initialize",
            """{"jsonrpc":"2.0","id":2,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""",
            """{"jsonrpc":"2.0","id":2,"result":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"serverInfo":{"name":"mcpifier","version":"1.0"}}}""",
            false
        );

        data.Add(
            "ToolsList",
            """{"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}""",
            """{"jsonrpc":"2.0","id":3,"result":{"tools":[{"name":"test_tool","description":"test tool","inputSchema":{"type":"object","required":[]}}]}}""",
            false
        );

        // === Success Scenarios ===

        data.Add(
            "ToolCall_Success",
            """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"test_tool","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":4,"result":{"content":[{"type":"text","text":"{\"status\":\"ok\"}"}]}}""",
            true
        );

        // === HTTP Error Scenarios ===

        data.Add(
            "ToolCall_404",
            """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"test_tool_404","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":5,"error":{"code":-32000,"message":"Not Found"}}""",
            true
        );

        data.Add(
            "ToolCall_500",
            """{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"test_tool_500","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":6,"error":{"code":-32000,"message":"Internal Server Error"}}""",
            true
        );

        // === Tool Not Found ===

        data.Add(
            "ToolCall_NonExistent",
            """{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"non_existent_tool","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":7,"error":{"code":-32601,"message":"Tool not found"}}""",
            false
        );

        // === Malformed JSON ===

        data.Add(
            "MalformedJson",
            """"{"jsonrpc":"2.0","id":1,"method":"ping"""",  // Missing closing brace
            """{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"Parse error"}}""",
            false
        );

        // === JSON-RPC Invalid Request Tests ===

        data.Add(
            "MissingJsonRpc",
            """{"id":1,"method":"ping"}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}""",
            false
        );

        data.Add(
            "InvalidMethod_NotString",
            """{"jsonrpc":"2.0","id":1,"method":123}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}""",
            false
        );

        data.Add(
            "MissingMethod",
            """{"jsonrpc":"2.0","id":1}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}""",
            false
        );

        data.Add(
            "MethodNotFound",
            """{"jsonrpc":"2.0","id":1,"method":"unknown_method","params":{}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"Method not found"}}""",
            false
        );

        // === Invalid Params Tests ===

        data.Add(
            "InvalidParams_String",
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":"invalid"}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}""",
            false
        );

        data.Add(
            "ToolCall_MissingName",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"arguments":{}}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}""",
            false
        );

        data.Add(
            "ToolCall_InvalidArguments_NotObject",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"test_tool","arguments":"invalid"}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}""",
            false
        );

        return data;
    }

    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public async Task HttpServer_EndToEnd_AllScenarios(
        string testName,
        string mcpRequest,
        string expectedResponse,
        bool shouldVerifyMockCalled)
    {
        // Arrange
        var mockResponses = new Dictionary<string, (HttpStatusCode, string)>
        {
            ["/api/test"] = (HttpStatusCode.OK, """{"status":"ok"}"""),
            ["/api/notfound"] = (HttpStatusCode.NotFound, """{"error":"not found"}"""),
            ["/api/servererror"] = (HttpStatusCode.InternalServerError, """{"error":"server error"}""")
        };

        var mockHandler = new MockHttpMessageHandler(mockResponses);

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<RestApiService>((sp, client) =>
                {
                    client.BaseAddress = new Uri("http://example.com");
                })
                .ConfigurePrimaryHttpMessageHandler(() => mockHandler);
            });
        }).CreateClient();

        // Act
        HttpResponseMessage response;
        string actualResponse;

        try
        {
            response = await client.PostAsync("/",
                new StringContent(mcpRequest, Encoding.UTF8, "application/json"));
            actualResponse = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Test '{testName}' failed during request: {ex.Message}", ex);
        }

        // Assert
        response.EnsureSuccessStatusCode();

        var expected = JsonDocument.Parse(expectedResponse);
        var actual = JsonDocument.Parse(actualResponse);

        AssertJsonEquals(expected.RootElement, actual.RootElement, testName);

        if (shouldVerifyMockCalled)
        {
            Assert.True(mockHandler.WasCalled, $"Test '{testName}': Expected mock HTTP handler to be called");
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
/// Mock HttpMessageHandler for testing outbound REST calls.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode status, string body)>? responses;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? customHandler;

    // Keep existing constructor for backward compatibility
    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        customHandler = handler;
    }

    // New constructor for dictionary-based responses
    public MockHttpMessageHandler(Dictionary<string, (HttpStatusCode status, string body)> responses)
    {
        this.responses = responses;
    }

    public bool WasCalled { get; private set; }
    public List<HttpRequestMessage> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        Requests.Add(request);

        // If custom handler provided, use it (backward compatibility)
        if (customHandler != null)
        {
            return await customHandler(request, cancellationToken);
        }

        // Otherwise use dictionary lookup
        string path = request.RequestUri?.PathAndQuery ?? "";

        foreach (var (key, (status, body)) in responses!)
        {
            if (path.Contains(key))
            {
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
            }
        }

        // Default fallback
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
    }
}
