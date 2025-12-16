# Summerdawn.Mcpify.Server.Tests

This project contains integration tests for the Mcpify server, demonstrating testability improvements that allow both HTTP and stdio modes to be tested in-process.

## Test Structure

### HTTP Integration Tests (`HttpIntegrationTests.cs`)
- Uses `WebApplicationFactory<ProgramHttp>` to host the server in-process
- Tests MCP JSON-RPC endpoints (tools/list, tools/call)
- Mocks outbound REST calls using custom `HttpMessageHandler`
- Verifies request/response handling and REST proxy behavior

### Stdio Integration Tests (`StdioIntegrationTests.cs`)
- Demonstrates the `IStdio` abstraction for injectable stdio streams
- Note: Currently skipped due to complexity of testing async stream reading
- Shows how `IStdio` can be replaced for testing purposes

## Key Components

### McpifyServerFactory
Custom `WebApplicationFactory` that:
- Uses `ProgramHttp.CreateHostBuilder()` for HTTP-only hosting
- Sets content root to test directory for test-specific configuration
- Allows service overrides for mocking dependencies

### MockHttpMessageHandler
Custom `HttpMessageHandler` for mocking outbound REST calls:
- Does not mock `HttpClient` directly (per best practices)
- Allows verification that REST calls were made
- Returns deterministic responses for testing

### TestStdio
Test implementation of `IStdio` using in-memory streams:
- Demonstrates stdio abstraction capability
- Enables in-process testing of stdio mode (with additional work)

## Running Tests

```bash
# Run all tests
dotnet test

# Run only HTTP tests
dotnet test --filter "FullyQualifiedName~HttpIntegrationTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Testability Improvements

The following changes enable comprehensive testing:

1. **Injectable Stdio Abstraction (`IStdio`)**: Allows replacing Console I/O with in-memory streams for testing
2. **ProgramHttp Entry Point**: Provides HTTP-only host builder that `WebApplicationFactory` can use
3. **Service Override Support**: Tests can replace `HttpClient` configuration to mock REST calls

## Test Configuration

Tests use `mappings.json` in the test project directory with a simple test tool definition:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "http://example.com"
    },
    "Tools": [
      {
        "mcp": {
          "name": "test_tool",
          "description": "A test tool for integration testing.",
          ...
        },
        "rest": {
          "method": "POST",
          "path": "/api/test",
          ...
        }
      }
    ]
  }
}
```
