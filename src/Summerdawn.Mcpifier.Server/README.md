# Summerdawn.Mcpifier.Server

Ready-to-run MCP server with stdio and HTTP support. 

Mcpifier is a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON or generate them automatically from Swagger/OpenAPI specifications, and Mcpifier translates requests between MCP clients and your REST service.

Mcpifier Server is a ready-to-run MCP server that:

- **Requires no coding** - Fully configured via JSON files or Swagger
- **Proxies to REST APIs** - Forwards MCP tool calls to your REST endpoints
- **Supports multiple transports** - HTTP and stdio modes
- **Handles authentication** - Forwards authorization headers to your API (HTTP mode) or uses default headers (both modes)
- **Generates tool mappings** - Auto-generate from Swagger/OpenAPI specifications

This server is perfect for quickly exposing REST APIs to MCP clients like Claude Desktop or VS Code without writing any integration code.

## Quickstart

### Quickstart 1: Serve Directly from Swagger

The fastest way to get started - no configuration files needed:

```bash
# Install
dotnet tool install -g Summerdawn.Mcpifier.Server

# Serve directly from Swagger (HTTP or stdio)
mcpifier serve --mode http --swagger https://api.example.com/swagger.json

# Or stdio mode
mcpifier serve --mode stdio --swagger https://api.example.com/swagger.json
```

### Quickstart 2: Generate, Customize, and Serve

For full control over tool mappings:

```bash
# Install
dotnet tool install -g Summerdawn.Mcpifier.Server

# Generate mappings from Swagger
mcpifier generate --swagger https://api.example.com/swagger.json

# This creates mappings.json - edit it to customize:
# - Tool names and descriptions
# - Add/remove endpoints
# - Adjust parameter schemas
# - Configure base address, headers

# Serve with your customized mappings
mcpifier serve --mode http
```

## Installation

### As a .NET Tool (Recommended)

Install globally:
```bash
dotnet tool install -g Summerdawn.Mcpifier.Server
```

Or locally in a project:
```bash
dotnet tool install Summerdawn.Mcpifier.Server
```

### As a Standalone Binary

Download pre-built binaries from [GitHub Releases](https://github.com/summerdawn-ai/mcpifier/releases) for:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Commands

### serve (Default Command)

Starts the Mcpifier server in HTTP or stdio mode.

**Usage:**
```bash
mcpifier serve --mode <http|stdio> [--swagger <file-or-url>]

# Or simply (serve is the default):
mcpifier --mode <http|stdio> [--swagger <file-or-url>]
```

**Options:**
- `--mode`, `-m` (required): Server mode - `http` or `stdio`
- `--swagger` (optional): File name or URL of Swagger/OpenAPI specification

**Examples:**
```bash
# HTTP mode with Swagger
mcpifier serve --mode http --swagger https://api.example.com/swagger.json

# stdio mode with Swagger
mcpifier serve --mode stdio --swagger swagger.json

# HTTP mode with mappings.json
mcpifier serve --mode http

# Short form (serve is default)
mcpifier --mode http --swagger swagger.json
```

### generate

Generates tool mappings from a Swagger/OpenAPI specification and saves to a JSON file.

**Usage:**
```bash
mcpifier generate --swagger <file-or-url> [--output <filename>]
```

**Options:**
- `--swagger` (required): File name or URL of Swagger/OpenAPI specification
- `--output` (optional): Custom output filename (default: `mappings.json`)

**Examples:**
```bash
# Generate from local file
mcpifier generate --swagger swagger.json

# Generate from URL
mcpifier generate --swagger https://api.example.com/swagger.json

# Custom output file
mcpifier generate --swagger swagger.json --output my-tools.json
```

**Generated Output Structure:**
```json
{
  "mcpifier": {
    "tools": [
      {
        "mcp": {
          "name": "get_user_by_id",
          "description": "Get user by ID",
          "inputSchema": { ... }
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

## Usage

The server starts on:
- HTTP: `http://localhost:5157`
- HTTPS: `https://localhost:7025`

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
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "User-Agent": "Mcpifier/1.0"
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

Contains tool definitions.  For complete documentation on tool structure, parameter interpolation, and examples, see the [main README Configuration section](https://github.com/summerdawn-ai/mcpifier#configuration).

**Brief example:**

```json
{
  "Mcpifier": {
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

See the [main README Configuration section](https://github.com/summerdawn-ai/mcpifier#configuration).

## Configuring MCP Clients

For complete MCP client setup instructions for VS Code, Claude Desktop, and other clients, see the [main README](https://github.com/summerdawn-ai/mcpifier#configuring-mcp-clients-for-mcpifier).

**Brief example for VS Code:**

```json
{
  "mcpServers": {
    "my-api": {
      "command": "mcpifier",
      "args": ["serve", "--mode", "stdio"],
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
      "Summerdawn.Mcpifier": "Debug"
    }
  }
}
```

## Troubleshooting

### Configuration Errors

**Problem**: "No tools mappings have been found in the configuration"<br>
**Solution**: Ensure `mappings.json` with at least one tool mapping exists in the directory specified by `DOTNET_CONTENTROOT` or the working directory.

**Problem**: "Failed to configure Mcpifier services"<br>
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
      "Summerdawn.Mcpifier": "Trace"
    }
  }
}
```

**Note**: Debug logs may contain sensitive information. Don't use in production.

## Further Documentation

- **Configuration Details**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuration)
- **MCP Client Setup**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuring-mcp-clients-for-mcpifier)
- **Architecture Overview**: [Main README](https://github.com/summerdawn-ai/mcpifier#architecture)
- **ASP.NET Core Integration**: [Summerdawn.Mcpifier.AspNetCore](../Summerdawn.Mcpifier.AspNetCore/README.md)
- **Core Library**: [Summerdawn.Mcpifier](../Summerdawn.Mcpifier/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpifier](https://github.com/summerdawn-ai/mcpifier)

## License

This project is licensed under the MIT License.
