# Summerdawn.Mcpify

Mcpify is a zero-code MCP (Model Context Protocol) proxy that exposes an existing REST API as an MCP server.

## Overview

Mcpify enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON, and Mcpify translates requests between MCP clients and your REST service.

This package provides the foundational components for building MCP servers that proxy requests to REST APIs:

- **JSON-RPC 2.0 message handling** - Parse and dispatch MCP protocol messages
- **MCP protocol implementation** - Server info, tool listing, and tool execution
- **REST API proxy service** - HTTP client with parameter interpolation
- **STDIO support** - Process-based communication via stdio

### When to Use This Package

Use `Summerdawn.Mcpify` when you need:
- Fine-grained control over MCP message handling
- Custom hosting scenarios beyond ASP.NET Core
- Integration with existing service architectures
- STDIO-based communication for process integration

**Most users should use `Summerdawn.Mcpify.AspNetCore` instead**, which provides HTTP support using ASP.NET Core.

## Installation

```bash
dotnet add package Summerdawn.Mcpify
```

## Dependencies

- **.NET 8.0** or later
- **Microsoft.Extensions.Http** - HTTP client factory
- **Microsoft.Extensions.Hosting.Abstractions** - Hosting abstractions
- **Microsoft.AspNetCore.Http.Abstractions** - HTTP context abstractions

## Usage

### Service Registration

```csharp
using Summerdawn.Mcpify.DependencyInjection;

// In your service configuration
services.AddMcpify(configuration.GetSection("Mcpify"));
```

### Basic Configuration Example

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "User-Agent": "MyApp-MCP/1.0"
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

For complete configuration documentation including authorization, header forwarding, and all available settings, see the [main README](https://github.com/summerdawn-ai/mcpify#configuration).

### Using with ASP.NET Core

If you're building an ASP.NET Core application, use the `Summerdawn.Mcpify.AspNetCore` package instead:

```bash
dotnet add package Summerdawn.Mcpify.AspNetCore
```

It provides simpler integration with endpoint routing and middleware.

## Configuration

For detailed configuration including: 
- All configuration settings and their meanings
- Tool mappings structure
- Parameter interpolation
- Authorization scenarios (MCP Authorization, default headers, forwarded headers)
- Loading from separate files
- Generating from OpenAPI specs

See the [main README Configuration section](https://github.com/summerdawn-ai/mcpify#configuration).

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

## Further Documentation

- **Configuration Details**: [Main README](https://github.com/summerdawn-ai/mcpify#configuration)
- **MCP Client Setup**: [Main README](https://github.com/summerdawn-ai/mcpify#configuring-mcp-clients-for-mcpify)
- **ASP.NET Core Integration**: [Summerdawn.Mcpify.AspNetCore](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify.AspNetCore/README.md)
- **Standalone Server**: [Summerdawn.Mcpify.Server](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify.Server/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpify](https://github.com/summerdawn-ai/mcpify)

## License

This project is licensed under the MIT License.
