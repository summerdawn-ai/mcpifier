using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Services;

using Xunit.Abstractions;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Integration tests for HTTP mode using WebApplicationFactory.
/// </summary>
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class HttpIntegrationTests(McpifierServerFactory factory, ITestOutputHelper output) : IClassFixture<McpifierServerFactory>
{
    private static readonly JsonSerializerOptions NormalizedJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static TheoryData<string, string, string> GetDataForIntegrationTests_Mcp() => new()
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

    public static TheoryData<string, string, string> GetDataForIntegrationTests_Http() => new()
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

    public static TheoryData<string, string, string> GetDataForIntegrationTests_Invalid() => new()
    {
        // Format: (scenario, mcpRequest, expectedResponse)
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
            """{"jsonrpc":"2.0","id":null,"error":{"code":-32600,"message":"Invalid Request"}}"""
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
            "ToolCall_NonExistent",
            """{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"non_existent_tool","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":7,"error":{"code":-32601,"message":"Tool not found"}}"""
        },
        {
            "InvalidParams_String",
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":"invalid"}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        },
        {
            "Error_ToolCall_MissingName",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"arguments":{}}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        },
        {
            "ToolCall_InvalidArguments_NotObject",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"test_tool","arguments":"invalid"}}""",
            """{"jsonrpc":"2.0","id":1,"error":{"code":-32602,"message":"Invalid params"}}"""
        }
    };

    [Theory]
    [MemberData(nameof(GetDataForIntegrationTests_Mcp))]
    public Task HandleRequest_WithMcpMethod_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(GetDataForIntegrationTests_Http))]
    public Task HandleRequest_WithToolCall_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(GetDataForIntegrationTests_Invalid))]
    public Task HandleRequest_WithInvalidRequest_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    private async Task HandleRequest_WithGivenRequest_ReturnsExpectedResponse(string scenario, string mcpRequest, string expectedResponse)
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
        string actualResponse;

        try
        {
            var response = await client.PostAsync("/", new StringContent(mcpRequest, Encoding.UTF8, "application/json"));
            actualResponse = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Test '{scenario}' failed during request: {ex.Message}", ex);
        }

        actualResponse = NormalizeJson(actualResponse);

        output.WriteLine("Scenario:          {0}", scenario);
        output.WriteLine("Request:           {0}", mcpRequest);
        output.WriteLine("Expected response: {0}", expectedResponse);
        output.WriteLine("Actual response:   {0}", actualResponse);

        Assert.Equal(expectedResponse, actualResponse);
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, NormalizedJsonOptions);
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
    public List<HttpRequestMessage> Requests { get; } = [];

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
        if (responses == null)
        {
            throw new InvalidOperationException("Either customHandler or responses must be provided");
        }

        string path = request.RequestUri?.PathAndQuery ?? "";

        foreach (var (key, (status, body)) in responses)
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
