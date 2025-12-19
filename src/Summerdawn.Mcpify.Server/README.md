# Summerdawn.Mcpify.Server

Standalone MCP server that proxies requests to REST APIs without requiring any code.

## Overview

Mcpify Server is a ready-to-run MCP (Model Context Protocol) server that:

- **Requires no coding** - Fully configured via JSON files
- **Proxies to REST APIs** - Forwards MCP tool calls to your REST endpoints
- **Supports multiple transports** - HTTP and stdio modes
- **Handles authentication** - Forwards authorization headers to your API

This server is perfect for quickly exposing REST APIs to MCP clients like Claude Desktop or VS Code without writing any integration code.

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

### As a Standalone Binary

Download pre-built binaries from [GitHub Releases](https://github.com/summerdawn-ai/mcpify/releases) for:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Running the Server

### HTTP Mode

For network-based MCP clients:

```bash
# If installed as a dotnet tool:
mcpify-server --mode http

# Or with downloaded binary:
./mcpify-server --mode http
```

The server starts on:
- HTTP: `http://localhost:5157`
- HTTPS: `https://localhost:7025`

### Stdio Mode

For process-based MCP clients (Claude Desktop, VS Code):

```bash
# If installed as a dotnet tool:
mcpify-server --mode stdio

# Or with downloaded binary:
./mcpify-server --mode stdio
```

## Configuration

Configuration is split between two files for flexibility:

### appsettings.json (Environment-Specific)

Contains environment-specific settings like API base URLs:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5157"
      },
      "Https": {
        "Url": "https://0.0.0.0:7025"
      }
    }
  },
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
      "Name": "my-mcp-server",
      "Title": "My MCP Server",
      "Version": "1.0.0"
    },
    "Authorization": {
      "RequireAuthorization": false
    }
  }
}
```

### mappings.json (Environment-Independent)

Contains tool definitions that work across all environments:

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

### Tool Definition Format

Each tool has two sections:

**MCP Section** - Defines the MCP tool interface:
```json
"mcp": {
  "name": "tool_name",
  "description": "What the tool does",
  "inputSchema": {
    "type": "object",
    "properties": {
      "param1": {
        "type": "string",
        "description": "Parameter description"
      }
    },
    "required": ["param1"]
  }
}
```

**REST Section** - Maps to REST API endpoint:
```json
"rest": {
  "method": "GET|POST|PUT|DELETE|PATCH",
  "path": "/endpoint/{param1}",
  "query": "key={param2}",
  "body": "{ \"field\": {param3} }"
}
```

### Parameter Interpolation

Use `{paramName}` to inject tool arguments:

- **Path**: `/users/{userId}/posts/{postId}`
- **Query**: `from={startDate}&to={endDate}`
- **Body**: `{ "name": {userName}, "email": {userEmail} }`

Arguments from the MCP tool call are automatically substituted.

## MCP Client Setup

### VS Code (.mcp.json)

Configure stdio mode for VS Code MCP extensions:

**Using dotnet tool (recommended)**:
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

**Using downloaded binary**:
```json
{
  "mcpServers": {
    "my-api": {
      "command": "/path/to/mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

**Using dotnet run for development**:
```json
{
  "mcpServers": {
    "my-api": {
      "command": "dotnet",
      "args": [
        "run",
        "--no-build",
        "--project",
        "path/to/Summerdawn.Mcpify.Server",
        "--mode",
        "stdio"
      ],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

**With Authorization Header Forwarding:**

If your REST API requires authentication, configure the client to send headers:

```json
{
  "mcpServers": {
    "my-api": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/server", "--mode", "stdio"],
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

And ensure forwarding is enabled in `appsettings.json`:

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

Edit Claude's configuration file:

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**Linux**: `~/.config/Claude/claude_desktop_config.json`

**Using dotnet tool (recommended)**:
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

**Using downloaded binary**:
```json
{
  "mcpServers": {
    "my-api": {
      "command": "/full/path/to/mcpify-server",
      "args": ["--mode", "stdio"],
      "env": {
        "DOTNET_CONTENTROOT": "/full/path/to/config/directory"
      }
    }
  }
}
```

**Important**: Use absolute paths for reliability.

### OAuth Configuration

For OAuth-protected APIs, configure the MCP client to handle authentication. The server will forward the `Authorization` header from the client to your REST API.

## MCP Endpoints (HTTP Mode)

When running in HTTP mode, the server exposes these endpoints:

### Server Info
```http
GET /
```

Returns server metadata:
```json
{
  "name": "my-mcp-server",
  "version": "1.0.0",
  "protocolVersion": "2024-11-05"
}
```

### List Tools
```http
POST /tools/list
Content-Type: application/json

{}
```

Returns all available tools from configuration.

### Call Tool
```http
POST /tools/call
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "get_user",
  "arguments": {
    "userId": "123"
  }
}
```

Executes the specified tool and returns the REST API response.

## Error Handling

The server passes through REST API errors unchanged:

**REST API Returns:**
```http
401 Unauthorized
{ "error": "invalid_token" }
```

**MCP Server Returns:**
```json
{
  "error": {
    "code": 401,
    "message": "REST API error",
    "data": {
      "status": 401,
      "body": {
        "error": "invalid_token"
      }
    }
  }
}
```

This allows MCP clients to see the actual error from your API.

## Security Considerations

1. **HTTPS Required**: Always use HTTPS in production to protect tokens in transit
2. **No Token Validation**: The server does NOT validate tokens - authentication is delegated to your REST API
3. **Header Forwarding**: Only headers explicitly configured in `ForwardedHeaders` are passed through
4. **Token Logging**: Tokens are never logged in full; only truncated versions appear in debug logs
5. **Trust Boundary**: The server trusts your REST API to handle authentication correctly

## Logging

The server logs:
- Startup configuration summary
- Tool count and names
- Each tool call (name, method, URL)
- REST API response status codes
- Authorization header presence (truncated for security)

**Configure logging level** in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Summerdawn.Mcpify": "Debug"
    }
  }
}
```

## Troubleshooting

### Configuration Errors

**Problem**: "mappings.json file not found"
**Solution**: Ensure `mappings.json` exists in the directory specified by `DOTNET_CONTENTROOT` or the working directory.

**Problem**: "Failed to configure Mcpify services"
**Solution**: Validate your JSON syntax in both configuration files.

### Connection Problems

**Problem**: Cannot connect to REST API
**Solution**: 
- Check `BaseAddress` in `appsettings.json`
- Verify network connectivity to the API
- Check firewall rules

### Authentication Issues

**Problem**: 401/403 errors from REST API
**Solution**:
- Ensure `Authorization` header is configured in `ForwardedHeaders`
- Verify the MCP client is sending the authorization token
- Check that the token is valid for your REST API

### Debug Logging

Enable detailed logging to diagnose issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Summerdawn.Mcpify": "Trace"
    }
  }
}
```

**Note**: Debug logs may contain sensitive information. Don't use in production.

## Development

### Building from Source

```bash
git clone https://github.com/summerdawn-ai/mcpify.git
cd mcpify
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## Further Documentation

- **Architecture Overview**: See the [main README](../../README.md) for detailed architecture information
- **ASP.NET Core Integration**: See [Summerdawn.Mcpify.AspNetCore](../Summerdawn.Mcpify.AspNetCore/README.md) for hosting within your own application
- **Core Library**: See [Summerdawn.Mcpify](../Summerdawn.Mcpify/README.md) for low-level API documentation
- **Source Code**: [GitHub repository](https://github.com/summerdawn-ai/mcpify)

## License

This project is licensed under the MIT License.
