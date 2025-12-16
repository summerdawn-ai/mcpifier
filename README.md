# Mcpify

**A zero-code MCP (Model Context Protocol) proxy for REST APIs**

Mcpify enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoints in JSON, and Mcpify handles all the protocol translation between MCP clients and your REST services.

## Overview

### What is Mcpify?

Mcpify is a configurable proxy that sits between MCP clients (like Claude Desktop, VS Code with MCP extensions, or other AI assistants) and REST APIs. It translates MCP tool calls into REST API requests and responses back into MCP format, all without requiring any code changes to your existing APIs.

### Key Benefits

- **Zero Code Required**: Define tools entirely through JSON configuration
- **No API Changes**: Works with existing REST APIs without modifications
- **Flexible Architecture**: Use as a library, ASP.NET Core middleware, or standalone server
- **Standards-Based**: Implements MCP protocol and JSON-RPC 2.0
- **Easy Integration**: Simple configuration for common scenarios

### Use Cases

- Expose internal REST APIs to AI assistants
- Enable Claude Desktop or VS Code to interact with your services
- Create MCP tools from OpenAPI/Swagger specifications
- Build AI-powered workflows with existing backend services
- Prototype MCP integrations quickly without custom code

## Repository Structure

- **[/src/Summerdawn.Mcpify](src/Summerdawn.Mcpify/README.md)** - Core library providing MCP protocol implementation, JSON-RPC handlers, and REST proxy service
- **[/src/Summerdawn.Mcpify.AspNetCore](src/Summerdawn.Mcpify.AspNetCore/README.md)** - ASP.NET Core integration for hosting Mcpify in your web applications
- **[/src/Summerdawn.Mcpify.Server](src/Summerdawn.Mcpify.Server/README.md)** - Standalone server for running Mcpify without coding
- **/tests** - Test projects for all libraries

## Key Technologies

- **.NET 8.0** - Modern cross-platform framework
- **ASP.NET Core** - For HTTP hosting scenarios
- **Model Context Protocol (MCP)** - Anthropic's protocol for AI-tool integration
- **JSON-RPC 2.0** - Standard RPC protocol used by MCP

## Architecture

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│             │   MCP   │             │   HTTP  │             │
│ MCP Client  │────────▶│   Mcpify    │────────▶│  REST API   │
│ (Claude,    │◀────────│   Proxy     │◀────────│             │
│  VS Code)   │         │             │         │             │
└─────────────┘         └─────────────┘         └─────────────┘
```

### The Three Packages

**Summerdawn.Mcpify (Core Library)**
- JSON-RPC message handling
- MCP protocol implementation
- REST API proxy service
- STDIO support for process-based communication

**When to use**: Building custom integrations or need fine-grained control over the MCP implementation.

**Summerdawn.Mcpify.AspNetCore (ASP.NET Core Integration)**
- HTTP endpoint mapping
- ASP.NET Core middleware
- Easy integration with existing web apps

**When to use**: Hosting Mcpify in your ASP.NET Core application (most common scenario).

**Summerdawn.Mcpify.Server (Standalone Server)**
- Pre-built executable
- HTTP and stdio transport modes
- Ready to run with just configuration files

**When to use**: Quick deployment without writing code, or as a standalone MCP server process.

## Configuration

### McpifyOptions Structure

Configuration is organized into several sections:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": { "User-Agent": "Mcpify/1.0" },
      "ForwardedHeaders": { "Authorization": true }
    },
    "ServerInfo": {
      "Name": "my-mcp-server",
      "Title": "My MCP Server",
      "Version": "1.0.0"
    },
    "Tools": [ /* tool definitions */ ]
  }
}
```

### Tool Mappings Structure

Each tool maps an MCP tool definition to a REST API endpoint:

```json
{
  "mcp": {
    "name": "get_user",
    "description": "Retrieve user information by ID",
    "inputSchema": {
      "type": "object",
      "properties": {
        "userId": {
          "type": "string",
          "description": "The user's unique identifier"
        }
      },
      "required": ["userId"]
    }
  },
  "rest": {
    "method": "GET",
    "path": "/users/{userId}",
    "query": "include=profile",
    "body": null
  }
}
```

### Parameter Interpolation

Use `{paramName}` placeholders to inject tool arguments into REST requests:

**In Path:**
```json
"path": "/users/{userId}/posts/{postId}"
```

**In Query String:**
```json
"query": "from={startDate}&to={endDate}&limit={maxResults}"
```

**In Request Body:**
```json
"body": "{ \"name\": {userName}, \"email\": {userEmail}, \"tags\": {tags} }"
```

Parameters are automatically interpolated from the MCP tool call's `arguments` object.

### Example: Complete mappings.json

Here's an actual example from the server:

```json
{
  "Mcpify": {
    "Tools": [
      {
        "mcp": {
          "name": "create_measurement",
          "description": "Create a new measurement entry.",
          "inputSchema": {
            "type": "object",
            "properties": {
              "timestampLocal": {
                "type": "string",
                "description": "Measurement timestamp in local time (ISO 8601).",
                "minLength": 1
              },
              "metrics": {
                "type": "object",
                "description": "Key/value metrics to record.",
                "minProperties": 1,
                "additionalProperties": {
                  "type": "integer",
                  "format": "int32"
                }
              }
            },
            "required": ["timestampLocal", "metrics"]
          }
        },
        "rest": {
          "method": "POST",
          "path": "/vibe/measurements",
          "body": "{ \"timestampLocal\": {timestampLocal}, \"metrics\": {metrics} }"
        }
      },
      {
        "mcp": {
          "name": "get_measurements",
          "description": "Retrieve measurements between two UTC timestamps.",
          "inputSchema": {
            "type": "object",
            "properties": {
              "from": {
                "type": "string",
                "description": "ISO 8601 start timestamp (inclusive).",
                "format": "date-time"
              },
              "to": {
                "type": "string",
                "description": "ISO 8601 end timestamp (exclusive).",
                "format": "date-time"
              }
            },
            "required": ["from", "to"]
          }
        },
        "rest": {
          "method": "GET",
          "path": "/vibe/measurements",
          "query": "from={from}&to={to}"
        }
      }
    ]
  }
}
```

### Loading Mappings from Separate Files

**Recommended approach** - Keep tool definitions separate from environment config:

```csharp
builder.Configuration.AddJsonFile("mappings.json");
builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"));
```

This keeps your tool definitions environment-independent and version-controllable.

### OpenAPI/Swagger Integration

For REST APIs with OpenAPI specifications, see Microsoft's documentation on [Swagger/OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger).

## Agent-Assisted Workflow

### Generating Mappings from OpenAPI Specs

Use an AI agent to convert OpenAPI/Swagger specifications into Mcpify mappings:

**Sample Prompt:**
```
Convert this OpenAPI endpoint specification into an Mcpify tool mapping in JSON format.

The output should have two sections:
1. "mcp": containing name, description, and inputSchema (JSON Schema)
2. "rest": containing method, path, query, and body

Use {paramName} syntax for parameter interpolation in path, query, and body.

OpenAPI Spec:
[paste your swagger.json excerpt here]
```

**Example Input (Swagger snippet):**
```json
{
  "/users/{id}": {
    "get": {
      "summary": "Get user by ID",
      "parameters": [
        {
          "name": "id",
          "in": "path",
          "required": true,
          "schema": { "type": "string" }
        }
      ]
    }
  }
}
```

**Example Output (Mcpify mapping):**
```json
{
  "mcp": {
    "name": "get_user",
    "description": "Get user by ID",
    "inputSchema": {
      "type": "object",
      "properties": {
        "id": {
          "type": "string",
          "description": "User identifier"
        }
      },
      "required": ["id"]
    }
  },
  "rest": {
    "method": "GET",
    "path": "/users/{id}"
  }
}
```

### Why Use Separate mappings.json?

- **Environment Independence**: Same tool definitions work across dev, staging, production
- **Version Control**: Track tool changes separately from environment config
- **Reusability**: Share mappings across different deployment scenarios
- **Clarity**: Cleaner separation of concerns

## Limitations

- **No Streaming Support**: Responses must be complete; streaming responses are not supported
- **No Server-Sent Events (SSE)**: Only request-response patterns are supported
- **JSON Only**: Binary data, file uploads, and non-JSON content types are not supported
- **Synchronous Only**: All REST calls are synchronous; no async/await patterns in mappings

## Quick Start

### For Library Users (ASP.NET Core)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load tool mappings
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpify services
builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"))
    .AddAspNetCore();

var app = builder.Build();

// Map Mcpify endpoint
app.MapMcpify("/mcp");

app.Run();
```

See [Summerdawn.Mcpify.AspNetCore README](src/Summerdawn.Mcpify.AspNetCore/README.md) for detailed scenarios.

### For Server Users

**HTTP Mode:**
```bash
cd src/Summerdawn.Mcpify.Server
dotnet run --mode http
```

**Stdio Mode:**

Build first to not pollute the stdio stream with build logs:

```bash
cd src/Summerdawn.Mcpify.Server
dotnet build
.\bin\Debug\net8.0\mcpify.exe --mode stdio
```

See [Summerdawn.Mcpify.Server README](src/Summerdawn.Mcpify.Server/README.md) for complete documentation.

## Using with MCP Clients

### VS Code (.mcp.json)

For stdio mode:
```json
{
  "mcpServers": {
    "my-api": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/Summerdawn.Mcpify.Server", "--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

If your REST API requires authentication, configure header forwarding:
```json
{
  "Mcpify": {
    "Rest": {
      "ForwardedHeaders": {
        "Authorization": true
      }
    }
  }
}
```

### Claude Desktop

Edit Claude's config file (location varies by OS):

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "my-api": {
      "command": "dotnet",
      "args": ["path/to/Summerdawn.Mcpify.Server.dll", "--mode", "stdio"]
    }
  }
}
```

### Generic MCP Clients

Mcpify supports both transport modes:
- **stdio**: For process-based clients (Claude Desktop, VS Code)
- **HTTP**: For network-based clients

Configure according to your client's requirements. All clients receive the same MCP protocol implementation.

## Links

### Documentation
- [Core Library (Summerdawn.Mcpify)](src/Summerdawn.Mcpify/README.md)
- [ASP.NET Core Integration (Summerdawn.Mcpify.AspNetCore)](src/Summerdawn.Mcpify.AspNetCore/README.md)
- [Standalone Server (Summerdawn.Mcpify.Server)](src/Summerdawn.Mcpify.Server/README.md)

### Downloads
- [GitHub Releases](https://github.com/summerdawn-ai/mcpify/releases) - Standalone server binaries
- [NuGet: Summerdawn.Mcpify](https://www.nuget.org/packages/Summerdawn.Mcpify) *(coming soon)*
- [NuGet: Summerdawn.Mcpify.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpify.AspNetCore) *(coming soon)*

### Resources
- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Security Policy](SECURITY.md)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
