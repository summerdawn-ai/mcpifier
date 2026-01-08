using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.AspNetCore.Tests;

public class McpRouteHandlerTests
{
    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsEmpty_Returns204()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonRpcResponse.Empty);
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        var request = new { jsonrpc = "2.0", method = "test.method" };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
        context.Request.ContentType = "application/json";

        // Act
        await handler.HandleMcpRequestAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsError_Returns200()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var errorResponse = JsonRpcResponse.MethodNotFound(JsonDocument.Parse("\"test-id\"").RootElement, "test.method");
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        // Act
        await handler.HandleMcpRequestAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsSuccess_Returns200()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var successResponse = JsonRpcResponse.Success(JsonDocument.Parse("\"test-id\"").RootElement, new McpToolsCallResult { Content = [JsonDocument.Parse("{ \"result\": \"success\" }").RootElement] });
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResponse);
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        // Act
        await handler.HandleMcpRequestAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherThrows_ThrowsException()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
        context.Request.ContentType = "application/json";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.HandleMcpRequestAsync(context));
    }

    [Fact]
    public async Task HandleMcpRequestAsync_InvalidJson_Returns200WithParseError()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        string invalidJson = "{ invalid json }";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        // Act
        await handler.HandleMcpRequestAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var responseJson = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        Assert.True(responseJson.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(JsonRpcResponse.ParseErrorCode, code.GetInt32());

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_NullRequest_Returns400WithInvalidRequest()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var mockOptions = CreateMockOptions();
        var mockLogger = new Mock<ILogger<McpRouteHandler>>();
        var handler = new McpRouteHandler(mockDispatcher.Object, mockOptions.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        string nullJson = "null";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(nullJson));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        // Act
        await handler.HandleMcpRequestAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var responseJson = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        Assert.True(responseJson.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(JsonRpcResponse.InvalidRequestCode, code.GetInt32());

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IOptions<McpifierOptions>> CreateMockOptions()
    {
        var options = new McpifierOptions();
        var mockOptions = new Mock<IOptions<McpifierOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        return mockOptions;
    }
}
