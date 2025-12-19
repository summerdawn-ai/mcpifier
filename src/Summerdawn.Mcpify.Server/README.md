# Summerdawn.Mcpify.Server

Ready-to-run MCP server with stdio and HTTP support. 

Mcpify is a zero-code MCP (Model Context Protocol) proxy that exposes an existing REST API as an MCP server.

## Overview

Mcpify enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON, and Mcpify translates requests between MCP clients and your REST service.

Mcpify Server is a ready-to-run MCP server that:

- **Requires no coding** - Fully configured via JSON files
- **Proxies to REST APIs** - Forwards MCP tool calls to your REST endpoints
- **Supports multiple transports** - HTTP and stdio modes
- **Handles authentication** - Forwards authorization headers to your API (HTTP mode) or uses default headers (both modes)

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

## Usage

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

Contains environment-specific settings like API base URLs and server configuration:

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

**Important Notes:**
- `ForwardedHeaders` - Only applies in HTTP mode. Headers cannot be forwarded from clients in stdio mode.
- `Authorization` section - Only applies in HTTP mode.

### mappings.json (Environment-Independent)

Contains tool definitions.  For complete documentation on tool structure, parameter interpolation, and examples, see the [main README Configuration section](https://github.com/summerdawn-ai/mcpify#configuration).

**Brief example:**

```json
{
  "Mcpify": {
    "Tools": [
      {
        "mcp": {
          "name": "get_user",
          "description": "Get user by ID",
          "inputSchema": {
            "type": "object",
            "properties": {
              "id": { "type": "string" }
            },
            "required": ["id"]
          }
        },
        "rest": {
          "method": "GET",
          "path": "/users/{id}"
        }
      }
    ]
  }
}
```

### Configuration Settings

For complete documentation of all configuration settings including: 
- `BaseAddress`, `DefaultHeaders`, `ForwardedHeaders`
- `ServerInfo` (Name, Title, Version)
- `Authorization` (RequireAuthorization, ResourceMetadata)
- Tool mappings and parameter interpolation
- Authorization scenarios

See the [main README Configuration section](https://github.com/summerdawn-ai/mcpify#configuration).

## Configuring MCP Clients

For complete MCP client setup instructions for VS Code, Claude Desktop, and other clients, see the [main README](https://github.com/summerdawn-ai/mcpify#configuring-mcp-clients-for-mcpify).

**Brief example for VS Code:**

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
3. **Header Forwarding** (HTTP mode only): Only headers explicitly configured in `ForwardedHeaders` are passed through
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

**Problem**: "No tools mappings have been found in the configuration"<br>
**Solution**: Ensure `mappings.json` with at least one tool mapping exists in the directory specified by `DOTNET_CONTENTROOT` or the working directory.

**Problem**: "Failed to configure Mcpify services"<br>
**Solution**: Validate your JSON syntax in both configuration files.

### Connection Problems

**Problem**: Cannot connect to REST API<br>
**Solution**: 
- Check `BaseAddress` in `appsettings.json`
- Verify network connectivity to the API
- Check firewall rules

### Authentication Issues

**Problem**: 401/403 errors from REST API<br>
**Solution**:
- For HTTP mode: Ensure `Authorization` header is configured in `ForwardedHeaders`
- For stdio mode: Use `DefaultHeaders` to include authorization
- Verify the MCP client is sending the authorization token (HTTP mode)
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

## Further Documentation

- **Configuration Details**: [Main README](https://github.com/summerdawn-ai/mcpify#configuration)
- **MCP Client Setup**: [Main README](https://github.com/summerdawn-ai/mcpify#configuring-mcp-clients-for-mcpify)
- **Architecture Overview**: [Main README](https://github.com/summerdawn-ai/mcpify#architecture)
- **ASP.NET Core Integration**: [Summerdawn.Mcpify.AspNetCore](../Summerdawn.Mcpify.AspNetCore/README.md)
- **Core Library**: [Summerdawn.Mcpify](../Summerdawn.Mcpify/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpify](https://github.com/summerdawn-ai/mcpify)

## License

This project is licensed under the MIT License.
