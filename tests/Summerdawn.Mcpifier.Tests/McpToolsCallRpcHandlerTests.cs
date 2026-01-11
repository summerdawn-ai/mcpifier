using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Handlers;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

using static JsonRpcResponse;

public class McpToolsCallRpcHandlerTests
{
    [Fact]
    public async Task Test_ToolNotFound_ReturnsInvalidParamsError()
    {
        // Arrange
        var options = CreateOptions([]);
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var restApiService = new RestApiService(httpClient, mockLogger.Object);
        var mockHandlerLogger = new Mock<ILogger<McpToolsCallRpcHandler>>();

        var handler = new McpToolsCallRpcHandler(
            restApiService,
            options,
            mockHandlerLogger.Object,
            null);

        var request = CreateRequest("nonexistent_tool", new Dictionary<string, JsonElement>());

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Equal(InvalidParamsCode, response.Error.Code);
        Assert.Contains("not found", JsonSerializer.Serialize(response.Error.Data));
    }

    [Fact]
    public async Task Test_InvalidArguments_ReturnsInvalidParamsError()
    {
        // Arrange
        var tool = CreateTestTool("test_tool", requiredProperties: ["message"]);
        var options = CreateOptions([tool]);
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var restApiService = new RestApiService(httpClient, mockLogger.Object);
        var mockHandlerLogger = new Mock<ILogger<McpToolsCallRpcHandler>>();

        var handler = new McpToolsCallRpcHandler(
            restApiService,
            options,
            mockHandlerLogger.Object,
            null);

        // Missing required "message" argument
        var request = CreateRequest("test_tool", new Dictionary<string, JsonElement>());

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.NotNull(response.Error);
        Assert.Equal(InvalidParamsCode, response.Error.Code);
    }

    [Fact]
    public async Task Test_RestApiError_ReturnsSuccessWithIsErrorTrue()
    {
        // Arrange
        var tool = CreateTestTool("test_tool", requiredProperties: ["message"]);
        var options = CreateOptions([tool]);
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        }));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var restApiService = new RestApiService(httpClient, mockLogger.Object);
        var mockHandlerLogger = new Mock<ILogger<McpToolsCallRpcHandler>>();

        var handler = new McpToolsCallRpcHandler(
            restApiService,
            options,
            mockHandlerLogger.Object,
            null);

        var arguments = new Dictionary<string, JsonElement>
        {
            ["message"] = JsonSerializer.SerializeToElement("test message")
        };
        var request = CreateRequest("test_tool", arguments);

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = JsonSerializer.SerializeToElement(response.Result).Deserialize<McpToolsCallResult>();

        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task Test_RestApiSuccess_ReturnsSuccessWithStructuredContent()
    {
        // Arrange
        var tool = CreateTestTool("test_tool", requiredProperties: ["message"]);
        var options = CreateOptions([tool]);
        string jsonResponse = "{\"status\":\"success\",\"data\":\"test\"}";
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        }));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var restApiService = new RestApiService(httpClient, mockLogger.Object);
        var mockHandlerLogger = new Mock<ILogger<McpToolsCallRpcHandler>>();

        var handler = new McpToolsCallRpcHandler(
            restApiService,
            options,
            mockHandlerLogger.Object,
            null);

        var arguments = new Dictionary<string, JsonElement>
        {
            ["message"] = JsonSerializer.SerializeToElement("test message")
        };
        var request = CreateRequest("test_tool", arguments);

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var result = JsonSerializer.SerializeToElement(response.Result).Deserialize<McpToolsCallResult>();

        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Content);
        Assert.NotNull(result.StructuredContent);

        // Verify structured content is parsed JSON
        var structuredContent = result.StructuredContent.Value;
        Assert.Equal(JsonValueKind.Object, structuredContent.ValueKind);
        Assert.True(structuredContent.TryGetProperty("status", out var statusProp));
        Assert.Equal("success", statusProp.GetString());
    }

    private static IOptions<McpifierOptions> CreateOptions(List<McpifierToolMapping> tools)
    {
        var options = new McpifierOptions
        {
            Tools = tools
        };
        return Options.Create(options);
    }

    private static McpifierToolMapping CreateTestTool(string name, string[]? requiredProperties = null)
    {
        return new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = name,
                Description = "Test tool",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, PropertySchema>
                    {
                        ["message"] = new PropertySchema
                        {
                            Type = "string",
                            Description = "Test message"
                        }
                    },
                    Required = requiredProperties?.ToList() ?? []
                }
            },
            Rest = new RestConfiguration
            {
                Method = "POST",
                Path = "/api/test",
                Body = "{ \"message\": {message} }"
            }
        };
    }

    private static JsonRpcRequest CreateRequest(string toolName, Dictionary<string, JsonElement> arguments)
    {
        var paramsObj = new McpToolsCallParams
        {
            Name = toolName,
            Arguments = arguments
        };

        return new JsonRpcRequest
        {
            Version = "2.0",
            Method = "tools/call",
            Id = JsonSerializer.SerializeToElement(1),
            Params = JsonSerializer.SerializeToElement(paramsObj)
        };
    }
}
