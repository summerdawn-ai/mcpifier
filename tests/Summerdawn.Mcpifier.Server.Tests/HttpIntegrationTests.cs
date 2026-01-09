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

    [Theory]
    [MemberData(nameof(StdioIntegrationTests.GetDataForHandleRequests_Mcp), MemberType = typeof(StdioIntegrationTests))]
    public Task HandleRequest_WithMcpMethod_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(StdioIntegrationTests.GetDataForHandleRequests_Http), MemberType = typeof(StdioIntegrationTests))]
    public Task HandleRequest_WithToolCall_ReturnsExpectedResponse(string testName, string mcpRequest, string expectedResponse) => HandleRequest_WithGivenRequest_ReturnsExpectedResponse(testName, mcpRequest, expectedResponse);

    [Theory]
    [MemberData(nameof(StdioIntegrationTests.GetDataForHandleRequests_Invalid), MemberType = typeof(StdioIntegrationTests))]
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
        var response = await client.PostAsync("/", new StringContent(mcpRequest, Encoding.UTF8, "application/json"));
        string actualResponse = await response.Content.ReadAsStringAsync();

        actualResponse = NormalizeJson(actualResponse);

        output.WriteLine("Scenario:          {0}", scenario);
        output.WriteLine("Request:           {0}", mcpRequest);
        output.WriteLine("Expected response: {0}", expectedResponse);
        output.WriteLine("Actual response:   {0}", actualResponse);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(expectedResponse, actualResponse);
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, NormalizedJsonOptions);
    }
}
