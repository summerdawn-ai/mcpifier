using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.Tests;

[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public class RestApiServiceTests
{
    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesPathParameters()
    {
        // Arrange
        string? capturedPath = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedPath = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "GET",
                Path = "/users/{userId}/posts/{postId}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["userId"] = JsonSerializer.SerializeToElement("123"),
            ["postId"] = JsonSerializer.SerializeToElement("456")
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedPath);
        Assert.Equal("/users/123/posts/456", capturedPath);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesPathParameters_WithSpecialCharacters()
    {
        // Arrange
        string? capturedPath = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedPath = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "GET",
                Path = "/search/{query}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonSerializer.SerializeToElement("hello world & friends")
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedPath);
        Assert.Equal("/search/hello%20world%20%26%20friends", capturedPath);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesQueryParameters()
    {
        // Arrange
        string? capturedQuery = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedQuery = request.RequestUri?.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "GET",
                Path = "/search",
                Query = "q={searchTerm}&limit={maxResults}&offset={startIndex}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["searchTerm"] = JsonSerializer.SerializeToElement("test query"),
            ["maxResults"] = JsonSerializer.SerializeToElement(10),
            ["startIndex"] = JsonSerializer.SerializeToElement(0)
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal("?q=test%20query&limit=10&offset=0", capturedQuery);
    }

    [Theory]
    [InlineData("q={query}", new[] { "query" }, "?q=query")]
    [InlineData("q={query}&sort={sortBy}", new[] { "query" }, "?q=query")]
    [InlineData("q={search}&limit={limit}", new[] { "search" }, "?q=search")]
    [InlineData("from={startDate}&to={endDate}&status={status}", new[] { "startDate", "status" }, "?from=startDate&status=status")]
    [InlineData("a={x}&b={y}&c={z}", new[] { "x", "z" }, "?a=x&c=z")]
    [InlineData("first={paramA}&middle={paramB}&last={paramC}", new[] { "paramB" }, "?middle=paramB")]
    [InlineData("x={arg}", new string[] { }, "?")]
    [InlineData("p1={a1}&p2={a2}&p3={a3}&p4={a4}", new[] { "a1", "a3" }, "?p1=a1&p3=a3")]
    [InlineData("v={weird&param}&limit={other weird}", new[] { "weird&param" }, "?v=weird%26param")]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesQueryParameters_WithMissingArguments(string query, string[] argumentNames, string expectedQuery)
    {
        // Arrange
        string? capturedQuery = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedQuery = request.RequestUri?.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        // Build arguments from the provided arg names
        var arguments = new Dictionary<string, JsonElement>();
        foreach (string argName in argumentNames)
        {
            arguments[argName] = JsonSerializer.SerializeToElement(argName);
        }

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "GET",
                Path = "/search",
                Query = query
            }
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal(expectedQuery, capturedQuery);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesBodyParameters()
    {
        // Arrange
        string? capturedBody = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "POST",
                Path = "/api/create",
                Body = "{\"name\":{name},\"age\":{age},\"active\":{active}}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("John Doe"),
            ["age"] = JsonSerializer.SerializeToElement(30),
            ["active"] = JsonSerializer.SerializeToElement(true)
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Equal("{\"name\":\"John Doe\",\"age\":30,\"active\":true}", capturedBody);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesBodyParameters_WithNestedObjects()
    {
        // Arrange
        string? capturedBody = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "POST",
                Path = "/api/create",
                Body = "{\"user\":{userObj},\"metadata\":{meta}}"
            }
        };

        var userObject = new { name = "Jane", email = "jane@example.com" };
        var metaObject = new { source = "test", timestamp = 123456789 };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["userObj"] = JsonSerializer.SerializeToElement(userObject),
            ["meta"] = JsonSerializer.SerializeToElement(metaObject)
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedBody);
        string expectedBody = "{\"user\":{\"name\":\"Jane\",\"email\":\"jane@example.com\"},\"metadata\":{\"source\":\"test\",\"timestamp\":123456789}}";
        Assert.Equal(expectedBody, capturedBody);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_InterpolatesBodyParameters_WithMissingArguments()
    {
        // Arrange
        string? capturedBody = null;
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "POST",
                Path = "/api/create",
                Body = "{\"name\":{name},\"optional\":{optionalField}}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Test")
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Equal("{\"name\":\"Test\",\"optional\":null}", capturedBody);
    }

    [Fact]
    [Obsolete]
    public async Task ExecuteToolAsync_CombinesPathQueryAndBodyInterpolation()
    {
        // Arrange
        string? capturedPath = null;
        string? capturedQuery = null;
        string? capturedBody = null;

        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            capturedPath = request.RequestUri?.AbsolutePath;
            capturedQuery = request.RequestUri?.Query;
            capturedBody = request.Content?.ReadAsStringAsync().Result;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            });
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
        var mockLogger = new Mock<ILogger<RestApiService>>();
        var service = new RestApiService(httpClient, mockLogger.Object);

        var tool = new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = "test_tool",
                Description = "Test tool",
                InputSchema = new InputSchema()
            },
            Rest = new RestConfiguration
            {
                Method = "POST",
                Path = "/users/{userId}/posts",
                Query = "draft={isDraft}",
                Body = "{\"title\":{title},\"content\":{content}}"
            }
        };

        var arguments = new Dictionary<string, JsonElement>
        {
            ["userId"] = JsonSerializer.SerializeToElement("user123"),
            ["isDraft"] = JsonSerializer.SerializeToElement(true),
            ["title"] = JsonSerializer.SerializeToElement("My Post"),
            ["content"] = JsonSerializer.SerializeToElement("This is the post content")
        };

        // Act
        await service.ExecuteToolAsync(tool, arguments, []);

        // Assert
        Assert.NotNull(capturedPath);
        Assert.NotNull(capturedQuery);
        Assert.NotNull(capturedBody);
        Assert.Equal("/users/user123/posts", capturedPath);
        Assert.Equal("?draft=True", capturedQuery);
        Assert.Equal("{\"title\":\"My Post\",\"content\":\"This is the post content\"}", capturedBody);
    }
}
