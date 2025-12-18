using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Summerdawn.Mcpify.Configuration;
using Summerdawn.Mcpify.Models;
using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.AspNetCore.Tests;

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
        Assert.Equal(204, context.Response.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsError_Returns400()
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
        Assert.Equal(400, context.Response.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsSuccess_Returns200()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var successResponse = JsonRpcResponse.Success(JsonDocument.Parse("\"test-id\"").RootElement, new { result = "success" });
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
        Assert.Equal(200, context.Response.StatusCode);
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

    private static Mock<IOptions<McpifyOptions>> CreateMockOptions()
    {
        var options = new McpifyOptions();
        var mockOptions = new Mock<IOptions<McpifyOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        return mockOptions;
    }
}
