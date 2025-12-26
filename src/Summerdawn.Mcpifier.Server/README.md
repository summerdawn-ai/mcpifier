# Summerdawn.Mcpifier.Server

Command-line server and tool for Mcpifier - a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier can be used as a library, ASP.NET Core middleware, or a command-line server and tool. It supports automatic tool generation from Swagger/OpenAPI specifications using conventions that map REST endpoints to MCP tools, or full customization using JSON configuration files.

This package provides a standalone command-line server and tool so that you can run Mcpifier in stdio or HTTP mode, or generate tool mappings from Swagger/OpenAPI specifications, without writing any code.

Other available packages:

- [Summerdawn.Mcpifier](https://www.nuget.org/packages/Summerdawn.Mcpifier): Core package with MCP implementation and stdio server
- [Summerdawn.Mcpifier.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpifier.AspNetCore): ASP.NET Core integration and HTTP server

## Getting Started

The fastest way to get started is to let Mcpifier generate tools from an existing Swagger/OpenAPI specification:

```bash
# Install
dotnet tool install -g Summerdawn.Mcpifier.Server

# Serve directly from Swagger (HTTP or stdio)
mcpifier serve --mode http --swagger https://api.example.com/swagger.json

# Or stdio mode
mcpifier serve --mode stdio --swagger https://api.example.com/swagger.json
```

When a Swagger/OpenAPI document is loaded, the REST API base address is inferred from the specification unless already configured.

Alternatively, use Mcpifier to generate a `mappings.json` file from a Swagger/OpenAPI specification, which you can then customize before serving:

```bash
# Install
dotnet tool install -g Summerdawn.Mcpifier.Server

# Generate mappings from Swagger
mcpifier generate --swagger https://api.example.com/swagger.json --output mappings.json

# Edit mappings.json to customize:
# - Tool names and descriptions
# - Add/remove endpoints
# - Adjust parameter schemas

# Serve with your customized mappings
mcpifier serve --mode http
```

Any `mappings.json` file in the configured content directory is automatically loaded when the server starts, so no additional arguments are needed.

See the [Usage](#usage) section below for a detailed description of available commands.

## Installation

### Installation as a .NET Tool

Install Mcpifier Server globally using the .NET CLI:

```bash
dotnet tool install -g Summerdawn.Mcpifier.Server
```

Or locally in a project:

```bash
dotnet tool install Summerdawn.Mcpifier.Server
```

### Installation as a Standalone Binary

You can download pre-built standalone binaries from Mcpifier's [GitHub Releases](https://github.com/summerdawn-ai/mcpifier/releases).

Supported platforms:

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Usage

The Mcpifier command-line server supports the following commands:

### mcpifier serve

The `serve`command starts Mcpifier as a server in HTTP or stdio mode.

**Usage:**

```bash
mcpifier serve --mode <http|stdio> [--swagger <file-or-url>]

# Or simply (serve is default)
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
mcpifier serve --mode stdio --swagger path/to/swagger.json

# HTTP mode with mappings.json
mcpifier serve --mode http

# Short form (serve is default)
mcpifier --mode http --swagger path/to/swagger.json
```

### mcpifier generate

The `generate` command generates tool mappings from a Swagger/OpenAPI specification and saves them to a JSON file, then exits.

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
mcpifier generate --swagger path/to/swagger.json

# Generate from URL
mcpifier generate --swagger https://api.example.com/swagger.json

# Custom output file
mcpifier generate --swagger path/to/swagger.json --output mappings-new.json
```

**Generated Output:**

```jsonc
{
  "Mcpifier": {
    "Rest": {
      // If available from the specification
      "BaseAddress": "https://api.example.com"
    },
    "Tools": [
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
      // etc.
    ]
  }
}
```

See the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/tree/main/src/Summerdawn.Mcpifier#tool-mapping) section for details on the tool mapping and the generated JSON file structure.

## Configuring MCP Clients

Mcpifier can be used with any MCP client that supports either stdio or HTTP transport modes. Below are configuration examples in the format that is used by most popular MCP clients, including Visual Studio, VS Code, and Claude.

### Mcpifier in stdio mode

To run the Mcpifier command-line server in stdio mode, add the following to your MCP client's configuration file:

```json
{
  "servers": {
    "my-stdio-mcpifier": {
      "type": "stdio",
      "command": "mcpifier",
      "args": ["serve", "--mode", "stdio", "--swagger", "path/to/swagger.json"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

### Mcpifier in HTTP mode

To connect to a running Mcpifier command-line server in HTTP mode, add the following to your MCP client's configuration file:

```json
{
  "servers": {
    "my-stdio-mcpifier": {
      "type": "http",
      "url": "https://localhost:7025",
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

By default, the Mcpifier command-line server is configured to listen on `https://localhost:7025` and `http://0.0.0.0:5157` in HTTP mode. You can change this by modifying the `Kestrel` section in `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5157"
      },
      "Https": {
        "Url": "https://localhost:7025"
      }
    }
  }
}
```

## Authorization

Mcpifier supports multiple strategies for handling authorization when accessing REST APIs that require it.

### Static Authorization via Default Headers

Use the `DefaultHeaders` configuration setting in `appsettings.json` to provide an authorization header that is included in every request. This works in stdio mode and HTTP mode:

```json
{
  "Mcpifier": {
    "Rest": {
      "DefaultHeaders": {
        "Authorization": "Bearer 123...abc"
      }
    }
  }
}
```

Instead of configuring this setting in `appsettings.json`, you can also [specify it as an environment variable](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider), for example:

```json
{
  "servers": {
    "my-stdio-mcpifier": {
      "type": "http",
      "url": "https://localhost:7025"
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config",
        "MCPIFIER__REST__DEFAULTHEADERS__AUTHORIZATION": "Bearer 123...abc"
      }
    }
  }
}
```

### Client-Provided Authorization via Forwarded Headers

Some MCP clients support configuring request headers for http mode MCP servers:

```json
{
  "servers": {
    "my-stdio-mcpifier": {
      "type": "http",
      "url": "https://localhost:5001",
      "headers": {
        "Authorization": "Bearer 123...abc"
      }
    }
  }
}
```

By default, the Mcpifier command-line server is configured to forward `Authorization` headers from the client to the REST API in HTTP mode. This can be configured in `appsettings.json`.

### OAuth with MCP Authorization

If your MCP client supports the MCP Authorization protocol, you can enable it in `appsettings.json` and allow the client to acquire and use an OAuth token, which Mcpifier will forward to the REST API:

```jsonc
{
  "Mcpifier": {
    "Rest": {
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "Authorization": {
      "RequireAuthorization": true,
      "ResourceMetadata": {
        "Resource": "https://mcp.example.com",
        "AuthorizationServers": ["https://auth.example.com/oauth/v2.0"],
        "ScopesSupported": ["https://mcp.example.com/access"]
      }
    }
  }
}
```

### Security Considerations

1. **HTTPS Required**: Always use HTTPS in production to protect tokens in transit
2. **No Token Validation**: The command-line server does NOT validate tokens itself - authentication is delegated to the REST API
3. **Trust Boundary**: The command-line server does not provide a trust boundary - it relies on the REST API to handle authentication and authorization correctly

For more details about authorization, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/tree/main/src/Summerdawn.Mcpifier#authorization) for stdio mode, and in the [Mcpifier ASP.NET Core documentation](https://github.com/summerdawn-ai/mcpifier/tree/main/src/Summerdawn.Mcpifier.AspNetCore#authorization) for HTTP mode.

## Configuration

The Mcpifier command-line server can be configured by modifying the `appsettings.json` and `mappings.json` files located in the content directory. The content directory can be specified by the `DOTNET_CONTENTROOT` environment variable, and defaults to the current working directory otherwise.

For the full list of settings, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/tree/main/src/Summerdawn.Mcpifier#configuration).

## Tool Mapping

Tool mapping configuration settings and interpolation rules are documented in the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/tree/main/src/Summerdawn.Mcpifier#tool-mapping).

## Logging

By default, the Mcpifier command-line server is configured to log:
- Startup configuration summary
- Tool count and names
- Each tool call (name, method, URL)
- Forwarded header names
- REST API response status codes

You can configure the logging level in `appsettings.json`:

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

See [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging) for more details on how to configure .NET logging.

## Troubleshooting

### Configuration Errors

**Problem**: Error "No tool mappings were found in the app configuration"<br>
**Solution**: Ensure `mappings.json` with at least one tool mapping exists in the directory specified by `DOTNET_CONTENTROOT` or the working directory, or use the `--swagger` option to load mappings from a Swagger/OpenAPI specification.

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
- Check that the specified credential is valid for your REST API

### Debug Logging

Enable detailed logging in `appsettings.json` to diagnose issues:

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

Debug logs may contain sensitive information. Don't enable them in a production environment.

## License

This project is licensed under the MIT License.
