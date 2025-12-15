using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Summerdawn.Mcpify.Services;

namespace Mcpify.Server.Tests;

/// <summary>
/// Integration tests for HTTP mode using WebApplicationFactory.
/// </summary>
public class HttpIntegrationTests : IClassFixture<McpifyServerFactory>
{
    private readonly McpifyServerFactory _factory;

    public HttpIntegrationTests(McpifyServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToolsListRequest_ReturnsExpectedTools()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the HttpClient for RestProxyService with a mock handler
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(HttpClient) && 
                    d.ImplementationFactory != null);
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Mock the HttpClient used by RestProxyService
                var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"status\":\"ok\"}")
                    });
                });

                services.AddHttpClient<RestProxyService>((sp, client) =>
                {
                    client.BaseAddress = new Uri("http://example.com");
                })
                .ConfigurePrimaryHttpMessageHandler(() => mockHandler);
            });
        }).CreateClient();

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { }
        };

        // Act
        var response = await client.PostAsJsonAsync("/", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.NotEmpty(tools.EnumerateArray());
    }

    [Fact]
    public async Task ToolsCallRequest_WithMockedHttpClient_ReturnsExpectedResponse()
    {
        // Arrange
        var expectedResponseBody = "{\"message\":\"test response\"}";
        var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            // Verify the request was made to the expected endpoint
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("/api/test", request.RequestUri?.ToString());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedResponseBody)
            });
        });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace HttpClient for RestProxyService
                var descriptor = services.SingleOrDefault(d => 
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IHttpClientFactory));
                
                services.AddHttpClient<RestProxyService>((sp, client) =>
                {
                    client.BaseAddress = new Uri("http://example.com");
                })
                .ConfigurePrimaryHttpMessageHandler(() => mockHandler);
            });
        }).CreateClient();

        var request = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "test_tool",
                arguments = new
                {
                    message = "test message"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/", request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        
        // If we get an error, output it for debugging
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with {response.StatusCode}: {content}");
        }
        
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("content", out var resultContent));
        
        // Verify the mock handler was called
        Assert.True(mockHandler.WasCalled);
    }
}

/// <summary>
/// Mock HttpMessageHandler for testing outbound REST calls.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    
    public bool WasCalled { get; private set; }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return await _handler(request, cancellationToken);
    }
}
