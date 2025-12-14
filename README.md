# Mcpify MCP-to-REST Proxy

Mcpify is a pure-proxy MCP (Model Context Protocol) server that forwards requests to REST APIs without validating JWT tokens. This server acts as a transparent pass-through, allowing MCP clients to interact with existing REST APIs while maintaining complete separation of concerns.

## Overview

Mcpify:
- **Does NOT validate** JWT tokens, signatures, audiences, issuers, or expiry
- **Does NOT enforce** authentication on incoming calls
- **Simply forwards** the `Authorization` header (if present) to the REST API unchanged
- **Passes through** all REST API responses (success or error) to the caller
- **Requires no code changes** to add new tools - only configuration updates

Authentication is handled **entirely by the REST API**, not by the MCP server.

## Features

- ✅ HTTPS support via Kestrel
- ✅ JSON-configured tool definitions
- ✅ Pure proxy behavior (no token validation)
- ✅ Authorization header pass-through
- ✅ Error pass-through from REST API
- ✅ Path parameter interpolation
- ✅ Query parameter support
- ✅ Request body support
- ✅ Input schema validation (required fields, basic type checking)
- ✅ Comprehensive logging (startup, tool calls, REST responses)
- ✅ MCP protocol compliance

## Quick Start

### 1. Configure API Base URL

Edit `appsettings.json` to set your REST API endpoint (environment-specific):

```json
{
  "ApiBaseUrl": "https://api.example.com"
}
```

### 2. Configure Tools

Edit `mappings.json` to define your tools (environment-independent). Note that the tools are nested under a `"mappings"` root element:

```json
{
  "mappings": {
    "defaultHeaders": {
      "User-Agent": "Summerdawn-MCP-RestProxy/1.0"
    },
    "tools": [
      {
        "name": "getUserProfile",
        "description": "Get the profile for a user.",
        "http": {
          "method": "GET",
          "path": "/users/{userId}",
          "query": {},
          "body": null
        },
        "inputSchema": {
          "type": "object",
          "properties": {
            "userId": {
              "type": "string",
              "description": "The unique identifier of the user"
            }
          },
          "required": ["userId"]
        },
        "output": {
          "mode": "rawJson"
        }
      }
    ]
  }
}
```

### 3. Run the Server

```bash
dotnet run --launch-profile https
```

The server will start on:
- HTTPS: `https://localhost:7026`
- HTTP: `http://localhost:5157` (fallback)

## MCP Endpoints

### Server Info
```
GET /
```

Returns server metadata including name, version, and protocol version.

### List Tools
```
POST /tools/list
Content-Type: application/json

{}
```

Returns all available tools from the configuration file.

### Call Tool
```
POST /tools/call
Content-Type: application/json
Authorization: Bearer <token>  # Optional, passed through to REST API

{
  "name": "getUserProfile",
  "arguments": {
    "userId": "user123"
  }
}
```

Executes the specified tool by forwarding the request to the REST API.

## Configuration Schema

### Configuration Structure

The configuration is split between two files:

**`appsettings.json`** - Environment-specific settings:
```json
{
  "ApiBaseUrl": "https://api.example.com"  // REST API base URL
}
```

**`mappings.json`** - Environment-independent tool definitions:
```json
{
  "mappings": {                             // Root element for tool mappings
    "defaultHeaders": {                     // Optional default headers
      "User-Agent": "Summerdawn-MCP-RestProxy/1.0"
    },
    "tools": [                              // Array of tool definitions
      { /* tool definition */ }
    ]
  }
}
```

### Tool Definition

```json
{
  "name": "toolName",           // Unique identifier for the tool
  "description": "...",          // Human-readable description
  "http": {
    "method": "GET|POST|PUT|DELETE|PATCH",
    "path": "/endpoint/{param}", // Path with {param} placeholders
    "query": {                   // Optional query parameters
      "key": "{value}"
    },
    "body": {                    // Optional request body
      "field": "{value}"
    }
  },
  "inputSchema": {               // JSON Schema for validation
    "type": "object",
    "properties": {
      "param": {
        "type": "string",
        "description": "..."
      }
    },
    "required": ["param"]
  },
  "output": {
    "mode": "rawJson"            // Output mode (currently only rawJson)
  }
}
```

### Parameter Interpolation

Use `{paramName}` placeholders in:
- Path: `/users/{userId}`
- Query: `{"filter": "{status}"}`
- Body: `{"name": "{userName}"}`

Arguments are automatically interpolated from the `arguments` object in the tool call request.

## Error Handling

The server passes through REST API errors unchanged:

**REST API Returns:**
```
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

## Logging

The server logs:
- Startup configuration summary
- Tool count and names
- Each tool call (name, method, URL)
- REST API response status codes
- Authorization header presence (truncated for security)

**Note:** Full JWT tokens are never logged. Authorization headers are truncated to first 30 characters.

## MCP Manifest

The MCP manifest file (`mcp/rest-proxy.mcp.json`) is provided for MCP clients to discover authentication requirements. The server itself does not enforce authentication.

Example:
```json
{
  "mcpServers": {
    "rest-proxy": {
      "name": "Summerdawn MCP REST Proxy",
      "transport": {
        "type": "https",
        "baseUrl": "https://localhost:7026"
      },
      "authentication": {
        "type": "oauth2",
        "oauth2": {
          "authorizationUrl": "https://auth.example.com/oauth/authorize",
          "tokenUrl": "https://auth.example.com/oauth/token",
          "scopes": ["openid", "profile", "api"]
        }
      }
    }
  }
}
```

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
# Test server info
curl -k https://localhost:7026/

# Test tools list
curl -k -X POST https://localhost:7026/tools/list \
  -H "Content-Type: application/json" \
  -d '{}'

# Test tool call
curl -k -X POST https://localhost:7026/tools/call \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"name":"getUserProfile","arguments":{"userId":"123"}}'
```

## Architecture

```
MCP Client
    ↓ (HTTPS + Authorization header)
MCP REST Proxy Server
    ↓ (forwards Authorization header unchanged)
REST API (handles authentication)
    ↓ (200 OK or 401/403/500 error)
MCP REST Proxy Server
    ↓ (passes through response)
MCP Client
```

## Security Considerations

1. **No Token Validation**: The server does not validate tokens. All authentication is delegated to the REST API.
2. **HTTPS Required**: Always use HTTPS in production to protect tokens in transit.
3. **Token Logging**: Tokens are never logged in full. Only truncated versions appear in debug logs.
4. **Trust Boundary**: The server trusts the REST API to handle authentication correctly.

## Adding New Tools

To add a new tool:

1. Edit `mappings.json`
2. Add a new tool definition to the `tools` array
3. Restart the server

**No code changes required!**

## License

Copyright © 2024 Summerdawn AI
