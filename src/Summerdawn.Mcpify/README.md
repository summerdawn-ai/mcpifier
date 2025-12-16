# Summerdawn.Mcpify

Core library providing MCP (Model Context Protocol) implementation and REST API proxy functionality.

## Overview

This package provides the foundational components for building MCP servers that proxy requests to REST APIs:

- **JSON-RPC 2.0 message handling** - Parse and dispatch MCP protocol messages
- **MCP protocol implementation** - Server info, tool listing, and tool execution
- **REST API proxy service** - HTTP client with parameter interpolation
- **STDIO support** - Process-based communication via `IStdio` abstraction

### When to Use This Package

Use `Summerdawn.Mcpify` when you need:
- Fine-grained control over MCP message handling
- Custom hosting scenarios beyond ASP.NET Core
- Integration with existing service architectures
- STDIO-based communication for process integration

**Most users should use `Summerdawn.Mcpify.AspNetCore` instead**, which provides easier integration with ASP.NET Core applications.

## Installation

```bash
dotnet add package Summerdawn.Mcpify
```

## Dependencies

- **.NET 8.0** or later
- **Microsoft.Extensions.Http** - HTTP client factory
- **Microsoft.Extensions.Hosting.Abstractions** - Hosting abstractions
- **Microsoft.AspNetCore.Http.Abstractions** - HTTP context abstractions

## Basic Usage

### Service Registration

```csharp
using Summerdawn.Mcpify.DependencyInjection;

// In your service configuration
services.AddMcpify(configuration.GetSection("Mcpify"));
```

### Configuration

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "User-Agent": "MyApp-MCP/1.0"
      },
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "ServerInfo": {
      "Name": "my-mcp-server",
      "Title": "My MCP Server",
      "Version": "1.0.0"
    },
    "Tools": [
      {
        "mcp": {
          "name": "get_data",
          "description": "Retrieve data from API",
          "inputSchema": {
            "type": "object",
            "properties": {
              "id": {
                "type": "string",
                "description": "Data identifier"
              }
            },
            "required": ["id"]
          }
        },
        "rest": {
          "method": "GET",
          "path": "/data/{id}"
        }
      }
    ]
  }
}
```

### Note for Most Users

If you're building an ASP.NET Core application, use the `Summerdawn.Mcpify.AspNetCore` package instead:

```bash
dotnet add package Summerdawn.Mcpify.AspNetCore
```

It provides simpler integration with endpoint routing and middleware.

## Configuration

### Rest Options

- **BaseAddress**: Base URL for the REST API
- **DefaultHeaders**: Headers sent with every request
- **ForwardedHeaders**: Headers forwarded from MCP client requests (e.g., `Authorization`)

### Tool Definition

Each tool requires:
- **mcp.name**: Unique tool identifier
- **mcp.description**: Human-readable description
- **mcp.inputSchema**: JSON Schema for input validation
- **rest.method**: HTTP method (GET, POST, PUT, DELETE, PATCH)
- **rest.path**: URL path with `{param}` placeholders
- **rest.query**: Optional query string with `{param}` placeholders
- **rest.body**: Optional request body with `{param}` placeholders

For detailed configuration documentation, see the [main README](https://github.com/summerdawn-ai/mcpify/blob/main/README.md#configuration).

## API Overview

### Key Classes and Services

**RestProxyService**
- Executes HTTP requests to REST APIs
- Handles parameter interpolation
- Manages headers (default and forwarded)

**JsonRpcDispatcher**
- Parses JSON-RPC 2.0 messages
- Routes requests to appropriate handlers
- Formats responses according to JSON-RPC spec

**IRpcHandler**
- Interface for implementing RPC method handlers
- Built-in handlers for MCP protocol methods:
  - `initialize` - Server capability negotiation
  - `tools/list` - List available tools
  - `tools/call` - Execute a tool

### STDIO Support

The `IStdio` abstraction enables process-based communication:

```csharp
public interface IStdio
{
    Task<string?> ReadLineAsync(CancellationToken cancellationToken = default);
    Task WriteLineAsync(string message, CancellationToken cancellationToken = default);
}
```

This allows MCP clients to communicate with your server via standard input/output, useful for:
- Claude Desktop integration
- VS Code MCP extensions
- Command-line based MCP clients

## Further Documentation

- **Detailed Configuration**: See the [main README](https://github.com/summerdawn-ai/mcpify/blob/main/README.md) for comprehensive configuration examples and parameter interpolation details
- **Source Code**: Visit the [GitHub repository](https://github.com/summerdawn-ai/mcpify)
- **ASP.NET Core Integration**: See [Summerdawn.Mcpify.AspNetCore](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify.AspNetCore/README.md)
- **Standalone Server**: See [Summerdawn.Mcpify.Server](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify.Server/README.md)

## License

This project is licensed under the MIT License.
