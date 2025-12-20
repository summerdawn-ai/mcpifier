using System;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpifier.Abstractions;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

public class McpStdioServerTests
{
    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsEmpty_NoResponseSent()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonRpcResponse.Empty);

        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var request = new { jsonrpc = "2.0", method = "test.method" };
        var requestJson = JsonSerializer.Serialize(request);
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act
        await server.HandleMcpRequestAsync(requestJson, writer, CancellationToken.None);

        // Assert
        await writer.FlushAsync();
        outputStream.Position = 0;
        var responseText = new StreamReader(outputStream).ReadToEnd();
        Assert.Empty(responseText); // No response for notifications

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsError_ErrorResponseSent()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var errorResponse = JsonRpcResponse.MethodNotFound(JsonDocument.Parse("\"test-id\"").RootElement, "test.method");
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);

        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        var requestJson = JsonSerializer.Serialize(request);
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act
        await server.HandleMcpRequestAsync(requestJson, writer, CancellationToken.None);

        // Assert
        await writer.FlushAsync();
        outputStream.Position = 0;
        var responseText = new StreamReader(outputStream).ReadToEnd();
        Assert.NotEmpty(responseText);

        var response = JsonDocument.Parse(responseText);
        Assert.True(response.RootElement.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(-32601, code.GetInt32()); // MethodNotFound

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_DispatcherReturnsSuccess_SuccessResponseSent()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var successResponse = JsonRpcResponse.Success(JsonDocument.Parse("\"test-id\"").RootElement, new McpToolsCallResult { Content = [JsonDocument.Parse("{ \"result\": \"success\" }").RootElement] });
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResponse);

        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        var requestJson = JsonSerializer.Serialize(request);
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act
        await server.HandleMcpRequestAsync(requestJson, writer, CancellationToken.None);

        // Assert
        await writer.FlushAsync();
        outputStream.Position = 0;
        var responseText = new StreamReader(outputStream).ReadToEnd();
        Assert.NotEmpty(responseText);

        var response = JsonDocument.Parse(responseText);
        Assert.True(response.RootElement.TryGetProperty("result", out var result));

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

        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var request = new { jsonrpc = "2.0", method = "test.method", id = "test-id" };
        var requestJson = JsonSerializer.Serialize(request);
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await server.HandleMcpRequestAsync(requestJson, writer, CancellationToken.None));
    }

    [Fact]
    public async Task HandleMcpRequestAsync_InvalidJson_ParseErrorResponseSent()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var invalidJson = "{ invalid json }";
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act
        await server.HandleMcpRequestAsync(invalidJson, writer, CancellationToken.None);

        // Assert
        await writer.FlushAsync();
        outputStream.Position = 0;
        var responseText = new StreamReader(outputStream).ReadToEnd();
        Assert.NotEmpty(responseText);

        var response = JsonDocument.Parse(responseText);
        Assert.True(response.RootElement.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(-32700, code.GetInt32()); // ParseError

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMcpRequestAsync_NullRequest_InvalidRequestResponseSent()
    {
        // Arrange
        var mockDispatcher = new Mock<IJsonRpcDispatcher>();
        var mockLogger = new Mock<ILogger<McpStdioServer>>();
        var mockStdio = new Mock<IStdio>();

        var server = new McpStdioServer(mockStdio.Object, mockDispatcher.Object, mockLogger.Object);

        var nullJson = "null";
        var outputStream = new MemoryStream();
        var writer = new StreamWriter(outputStream) { AutoFlush = true };

        // Act
        await server.HandleMcpRequestAsync(nullJson, writer, CancellationToken.None);

        // Assert
        await writer.FlushAsync();
        outputStream.Position = 0;
        var responseText = new StreamReader(outputStream).ReadToEnd();
        Assert.NotEmpty(responseText);

        var response = JsonDocument.Parse(responseText);
        Assert.True(response.RootElement.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(-32600, code.GetInt32()); // InvalidRequest

        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}