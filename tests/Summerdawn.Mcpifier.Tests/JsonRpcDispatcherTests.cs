using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpifier.Handlers;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

using static JsonRpcResponse;

public class JsonRpcDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        var expectedResponse = Success(JsonDocument.Parse("\"test-id\"").RootElement, new { result = "success" });
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(method => method == "test.method" ? mockHandler.Object : null, mockLogger.Object);

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
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(_ => mockHandler.Object, mockLogger.Object);

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
        Assert.Equal(InvalidRequestCode, response.Error.Code);
        Assert.Equal("Invalid Request", response.Error.Message);
        mockHandler.Verify(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_UnknownMethod_ReturnsMethodNotFoundError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(_ => null, mockLogger.Object); // No handler found

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
        Assert.Equal(MethodNotFoundCode, response.Error.Code);
        Assert.Contains("not found", response.Error.Message);
        Assert.Contains("unknown.method", JsonSerializer.Serialize(response.Error.Data));
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrowsJsonException_ReturnsInvalidParamsError()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Invalid parameter format"));

        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(_ => mockHandler.Object, mockLogger.Object);

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
        Assert.Equal(InvalidParamsCode, response.Error.Code);
        Assert.Contains("Invalid params", response.Error.Message);
        Assert.Contains("Invalid parameter format", JsonSerializer.Serialize(response.Error.Data));
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrowsException_ReturnsInternalError()
    {
        // Arrange
        var mockHandler = new Mock<IRpcHandler>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var mockLogger = new Mock<ILogger<JsonRpcDispatcher>>();
        var dispatcher = new JsonRpcDispatcher(_ => mockHandler.Object, mockLogger.Object);

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
        Assert.Equal(InternalErrorCode, response.Error.Code);
        Assert.Contains("Internal error", response.Error.Message);
        Assert.Contains("Something went wrong", JsonSerializer.Serialize(response.Error.Data));
    }
}
