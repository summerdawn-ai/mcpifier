using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpifier.Handlers;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

public class JsonRpcDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        var expectedResponse = JsonRpcResponse.Success(JsonDocument.Parse("\"test-id\"").RootElement, new { result = "success" });
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        Func<string, IRpcHandler?> handlerFactory = method => method == "test.method" ? mockHandler.Object : null;
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(handlerFactory, mockLogger.Object);

        var request = new JsonRpcRequest
        {
            Version = "2.0",
            Method = "test.method",
            Id = JsonDocument.Parse("\"test-id\"").RootElement
        };

        // Act
        var response = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        mockHandler.Verify(h => h.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_InvalidVersion_ReturnsInvalidRequestError()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        Func<string, IRpcHandler?> handlerFactory = method => mockHandler.Object;
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(handlerFactory, mockLogger.Object);

        var request = new JsonRpcRequest
        {
            Version = "1.0", // Invalid version
            Method = "test.method",
            Id = JsonDocument.Parse("\"test-id\"").RootElement
        };

        // Act
        var response = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32600, response.Error.Code);
        Assert.Equal("Invalid Request", response.Error.Message);
        mockHandler.Verify(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_UnknownMethod_ReturnsMethodNotFoundError()
    {
        // Arrange
        Func<string, IRpcHandler?> handlerFactory = method => null; // No handler found
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(handlerFactory, mockLogger.Object);

        var request = new JsonRpcRequest
        {
            Version = "2.0",
            Method = "unknown.method",
            Id = JsonDocument.Parse("\"test-id\"").RootElement
        };

        // Act
        var response = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error.Code);
        Assert.Contains("unknown.method", response.Error.Message);
        Assert.Contains("not found", response.Error.Message);
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrowsJsonException_ReturnsInvalidParamsError()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Invalid parameter format"));

        Func<string, IRpcHandler?> handlerFactory = method => mockHandler.Object;
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(handlerFactory, mockLogger.Object);

        var request = new JsonRpcRequest
        {
            Version = "2.0",
            Method = "test.method",
            Id = JsonDocument.Parse("\"test-id\"").RootElement
        };

        // Act
        var response = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32602, response.Error.Code);
        Assert.Contains("Invalid params", response.Error.Message);
        Assert.Contains("Invalid parameter format", response.Error.Message);
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrowsException_ReturnsInternalError()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        Func<string, IRpcHandler?> handlerFactory = method => mockHandler.Object;
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(handlerFactory, mockLogger.Object);

        var request = new JsonRpcRequest
        {
            Version = "2.0",
            Method = "test.method",
            Id = JsonDocument.Parse("\"test-id\"").RootElement
        };

        // Act
        var response = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32603, response.Error.Code);
        Assert.Contains("Internal error", response.Error.Message);
        Assert.Contains("Something went wrong", response.Error.Message);
    }
}
