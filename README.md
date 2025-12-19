# Mcpify

Mcpify is a zero-code MCP (Model Context Protocol) proxy that exposes an existing REST API as an MCP server.

## Overview

Mcpify enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON, and Mcpify translates requests between MCP clients and your REST service.

### Key Benefits

- **Zero Code Required** - Define tools entirely through JSON configuration
- **No API Changes** - Works with existing REST APIs without modifications
- **Standards-Based** - Implements MCP protocol and JSON-RPC 2.0 including MCP Authorization
- **Multiple Transports** - Supports both HTTP and stdio communication
- **Flexible Architecture** - Use as a library, ASP.NET Core middleware, or standalone server
- **Cross-Platform** - Runs on Windows, Linux, and macOS
- **Easy Integration** - Simple configuration for common scenarios
- **Open Source** - Fully open source under the MIT License

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
- STDIO server support for process-based communication

**When to use**: Hosting Mcpify as a stdio server, building custom integrations, or needing fine-grained control over the MCP implementation.

**Summerdawn.Mcpify.AspNetCore (ASP.NET Core Integration)**
- HTTP endpoint mapping
- ASP.NET Core middleware
- Easy integration with existing web apps

**When to use**: Hosting Mcpify as an http server, or integrating into an ASP.NET Core application.

**Summerdawn.Mcpify.Server (Standalone Server)**
- Available as a dotnet tool: `dotnet tool install -g Summerdawn.Mcpify.Server`
- Pre-built binaries for Windows, Linux, and macOS (x64 and ARM64)
- HTTP and stdio transport modes
- Ready to run with just configuration files

**When to use**: Quick deployment without writing code, or as a standalone MCP server process.

## Installation

### As a .NET Tool (Recommended)

Install globally:
```bash
dotnet tool install -g Summerdawn.Mcpify.Server
```

Or locally in a project:
```bash
dotnet tool install Summerdawn.Mcpify.Server
```

Then run:
```bash
mcpify-server --mode stdio
```

### As a Standalone Binary

Download pre-built binaries from [GitHub](https://github.com/summerdawn-ai/mcpify/releases) for:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

### As NuGet Packages

Install the core library or ASP.NET Core integration:
```bash
dotnet add package Summerdawn.Mcpify
dotnet add package Summerdawn.Mcpify.AspNetCore
```

## Configuration

### Structure

Mcpify configuration is organized into several sections:

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
    "Authorization": {
      "RequireAuthorization": false,
      "ResourceMetadata": {
        "Resource": "https://mcp.example.com",
        "AuthorizationServers": [ "https://auth.example.com/oauth" ],
        "ScopesSupported": [ "https://mcp.example.com/access" ]
      }
    },
    "Tools": [ /* tool definitions */ ]
  }
}
```

### Reference

#### Rest Configuration

**`BaseAddress`** (string, required)
- Base URL for the target REST API
- All tool requests will be made relative to this URL
- Example: `"https://api.example.com"`

**`DefaultHeaders`** (object, optional)
- Headers included in every REST API request
- Applies to both stdio and HTTP modes
- Static values set at configuration time
- Useful for API keys, user agents, or other fixed headers
- Example: 
  ```json
  "DefaultHeaders": {
    "User-Agent": "Mcpify/1.0",
    "X-API-Key": "your-api-key-here",
    "Accept": "application/json"
  }
  ```

**`ForwardedHeaders`** (object, optional) - **HTTP mode only**
- Headers forwarded from the MCP client request to the REST API
- Dynamic values provided by the client per request
- Each property name is a header name; set value to `true` to enable forwarding
- Common use case: forwarding `Authorization` header for OAuth flows
- Example:
  ```json
  "ForwardedHeaders": {
    "Authorization": true,
    "X-Custom-Header": true
  }
  ```
- **Note**: This setting only applies when running in HTTP mode. In stdio mode, headers cannot be forwarded from the client.

#### ServerInfo Configuration

**`Name`** (string, required)
- Unique identifier for the MCP server
- Used in MCP protocol handshake
- Should be lowercase with hyphens (e.g., `"my-api-server"`)

**`Title`** (string, optional)
- Human-readable server name
- Displayed to users in MCP clients

**`Version`** (string, optional)
- Server version number
- Useful for tracking deployments

#### Authorization Configuration - **HTTP mode only**

**`RequireAuthorization`** (boolean, optional, default: `false`) - **HTTP mode only**
- When `true`, enables MCP Authorization flow
- Unauthorized requests receive a 401 response with `WWW-Authenticate` header
- The `WWW-Authenticate` header directs clients to `/.well-known/oauth-protected-resource`
- Requires `ResourceMetadata` to be configured
- **Note**: This setting only applies when running in HTTP mode.

**`ResourceMetadata`** (object, optional) - **HTTP mode only**
- OAuth 2.0 Protected Resource Metadata served at `/.well-known/oauth-protected-resource`
- Required when `RequireAuthorization` is `true`
- Format follows [OAuth 2.0 Protected Resource Metadata](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-resource-metadata)
- Required fields:
  - `Resource` (string): The protected resource identifier (typically your MCP server's base URL)
  - `AuthorizationServers` (array of strings): Array of OAuth 2.0 authorization server URLs
  - `ScopesSupported` (array of strings, optional): Array of OAuth 2.0 scope values supported by the resource
- Example: 
  ```json
  "ResourceMetadata": {
    "Resource": "https://mcp.example.com",
    "AuthorizationServers": [ "https://auth.example.com/oauth" ],
    "ScopesSupported": [ "https://mcp.example.com/access" ]
  }
  ```
- See the [MCP Authorization specification](https://modelcontextprotocol.io/docs/tutorials/security/authorization) for complete details

#### Tools Configuration

**`Tools`** (array, required)
- Array of tool definitions
- Each tool maps an MCP tool to a REST API endpoint
- See [Tool Mappings Structure](#tool-mappings-structure) below

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

**MCP Section:**
- `name`: Unique tool identifier (required)
- `description`: Human-readable description (required)
- `inputSchema`: JSON Schema defining input parameters (required)

**REST Section:**
- `method`: HTTP method - GET, POST, PUT, DELETE, PATCH (required)
- `path`: URL path with `{param}` placeholders (required)
- `query`: Query string with `{param}` placeholders (optional)
- `body`: Request body template with `{param}` placeholders (optional)

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

### Authorization Scenarios

Mcpify supports multiple authorization scenarios: 

#### Scenario 1: Static API Key (Default Headers)

Use `DefaultHeaders` to include a static API key with every REST request:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "X-API-Key": "your-api-key-here"
      }
    }
  }
}
```

Works in both stdio and HTTP modes.

#### Scenario 2: OAuth with MCP Authorization (HTTP mode only)

Enable MCP Authorization to have clients authenticate via OAuth:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "Authorization": {
      "RequireAuthorization": true,
      "ResourceMetadata": {
        "Resource": "https://mcp.example.com",
        "AuthorizationServers": [ "https://auth.example.com/oauth/v2.0" ],
        "ScopesSupported": [ "https://mcp.example.com/access" ]
      }
    }
  }
}
```

When `RequireAuthorization` is `true`:
1. Unauthorized requests receive a 401 response
2. MCP clients discover the OAuth endpoints at `/.well-known/oauth-protected-resource`
3. Clients obtain tokens and include them in subsequent requests
4. Mcpify forwards the `Authorization` header to your REST API

See the [MCP Authorization specification](https://modelcontextprotocol.io/docs/tutorials/security/authorization) for complete OAuth flow details.

#### Scenario 3: Client-Provided Authorization (HTTP mode only)

Forward authorization headers from the MCP client without requiring MCP Authorization:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "Authorization": {
      "RequireAuthorization": false
    }
  }
}
```

The client is responsible for including a valid `Authorization` header in each request.

### Example: Complete Configuration

Here's a complete example configuration: 

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "User-Agent": "Mcpify/1.0"
      },
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "ServerInfo": {
      "Name": "example-api-server",
      "Title": "Example API Server",
      "Version": "1.0.0"
    },
    "Authorization": {
      "RequireAuthorization": true,
      "ResourceMetadata": {
        "Resource": "https://mcp.example.com",
        "AuthorizationServers": [ "https://auth.example.com/oauth/v2.0" ],
        "ScopesSupported": [ "https://mcp.example.com/access" ]
      }
    },
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

## Quick Start

### For Library Users

**Stdio:**

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Load tool mappings
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpify services
builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"));

// Send all console logging output to stderr so that it doesn't interfere with MCP stdio traffic.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var app = builder.Build();

// Use stdio MCP proxy.
app.UseMcpify();

app.Run();
```

See [Summerdawn.Mcpify README](src/Summerdawn.Mcpify/README.md) for detailed scenarios.

**ASP.NET Core:**

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
mcpify-server --mode http
```

**Stdio Mode:**
```bash
mcpify-server --mode stdio
```

See [Summerdawn.Mcpify.Server README](src/Summerdawn.Mcpify.Server/README.md) for complete documentation.

## Configuring MCP Clients for Mcpify

### VS Code (.mcp.json)

For stdio mode:
```json
{
  "mcpServers": {
    "my-api": {
      "command": "mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

If your REST API requires authentication, you have several options:

**Option 1: Static API Key (DefaultHeaders)**

Configure a static API key in `appsettings.json`:
```json
{
  "Mcpify": {
    "Rest": {
      "DefaultHeaders": {
        "X-API-Key": "your-api-key-here"
      }
    }
  }
}
```

**Option 2: Client-Provided Token**

Some MCP clients support providing authentication tokens: 
```json
{
  "mcpServers": {
    "my-api": {
      "command": "mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      },
      "auth": {
        "type": "bearer",
        "token": "your-token-here"
      }
    }
  }
}
```

**Note**: Header forwarding (`ForwardedHeaders`) is only available in HTTP mode.

### Claude Desktop

Edit Claude's config file (location varies by OS):

- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **Linux**: `~/.config/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "my-api": {
      "command": "mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "/full/path/to/config/directory"
      }
    }
  }
}
```

**With Authentication:**

```json
{
  "mcpServers": {
    "my-api": {
      "command": "mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "/full/path/to/config/directory"
      },
      "auth": {
        "type": "bearer",
        "token": "your-token-here"
      }
    }
  }
}
```

### Generic MCP Clients

Mcpify supports both transport modes:
- **stdio**: For process-based clients (Claude Desktop, VS Code)
- **HTTP**: For network-based clients

Configure according to your client's requirements. All clients receive the same MCP protocol implementation.

## Limitations

- **No Streaming Support**: Responses must be complete; streaming responses are not supported
- **No Server-Sent Events (SSE)**: Only request-response patterns are supported
- **JSON Only**: Binary data, file uploads, and non-JSON content types are not supported

## Links

### Documentation
- [Core Library (Summerdawn.Mcpify)](src/Summerdawn.Mcpify/README.md)
- [ASP.NET Core Integration (Summerdawn.Mcpify.AspNetCore)](src/Summerdawn.Mcpify.AspNetCore/README.md)
- [Standalone Server (Summerdawn.Mcpify.Server)](src/Summerdawn.Mcpify.Server/README.md)

### Downloads
- [GitHub Releases](https://github.com/summerdawn-ai/mcpify/releases) - Standalone server binaries
- [NuGet: Summerdawn.Mcpify](https://www.nuget.org/packages/Summerdawn.Mcpify)
- [NuGet: Summerdawn.Mcpify.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpify.AspNetCore)

### Resources
- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [MCP Authorization](https://modelcontextprotocol.io/docs/tutorials/security/authorization)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Security Policy](SECURITY.md)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
