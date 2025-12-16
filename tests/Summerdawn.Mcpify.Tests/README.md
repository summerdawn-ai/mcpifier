# Summerdawn.Mcpify.Tests

This project contains unit tests for the core Mcpify library, focusing on JSON-RPC handlers and REST proxy functionality.

## Test Structure

### Handler Tests (`McpToolsCallRpcHandlerTests.cs`)
- Tests the `McpToolsCallRpcHandler` which processes MCP `tools/call` requests
- Verifies tool lookup, argument validation, and error handling
- Uses real `RestProxyService` with mocked `HttpClient` for HTTP call verification
- Tests successful and error scenarios with structured content responses

### REST Proxy Service Tests (`RestProxyServiceTests.cs`)
- Tests the `RestProxyService` which proxies MCP tool calls to REST APIs
- Verifies path, query, and body parameter interpolation
- Tests URL encoding, special character handling, and missing parameter scenarios
- Covers edge cases like nested objects and combined interpolation

## Key Components

### MockHttpMessageHandler
Custom `HttpMessageHandler` for mocking outbound REST calls:
- Captures HTTP request details (path, query, body) for verification
- Returns deterministic responses for testing
- Follows best practices by mocking at the handler level, not `HttpClient` directly

## Running Tests

```bash
# Run all tests
dotnet test

# Run only handler tests
dotnet test --filter "FullyQualifiedName~McpToolsCallRpcHandlerTests"

# Run only REST proxy tests
dotnet test --filter "FullyQualifiedName~RestProxyServiceTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

### McpToolsCallRpcHandlerTests
- **Tool Not Found**: Verifies 404 error when tool doesn't exist
- **Invalid Arguments**: Verifies 400 error for missing required parameters
- **REST API Error**: Verifies error responses are wrapped correctly
- **REST API Success**: Verifies successful responses with structured JSON content

### RestProxyServiceTests
- **Path Interpolation**: Basic parameter substitution and URL encoding
- **Query Interpolation**: Multiple parameters and missing argument handling
- **Body Interpolation**: JSON values, nested objects, and null handling
- **Combined Interpolation**: All three methods working together

## Test Approach

Tests use real service instances with mocked HTTP infrastructure:

1. **Real Service Instances**: Uses actual `RestProxyService` and `McpToolsCallRpcHandler` instances
2. **Mocked HTTP Layer**: Uses `MockHttpMessageHandler` to control HTTP responses
3. **Request Verification**: Captures and verifies actual HTTP requests sent by the service
4. **Edge Case Coverage**: Tests special characters, missing parameters, and type conversions

## Example Test Pattern

```csharp
// Arrange
var mockHandler = new MockHttpMessageHandler((request, cancellationToken) =>
{
    // Verify request and return mock response
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"result\":\"ok\"}")
    });
});

var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://example.com") };
var service = new RestProxyService(httpClient, mockLogger.Object);

// Act
var result = await service.ExecuteToolAsync(tool, arguments, []);

// Assert
Assert.True(result.success);
```

## Dependencies

- **xUnit**: Test framework
- **Moq**: Mocking library for `ILogger` instances
- **System.Text.Json**: JSON serialization for test data
