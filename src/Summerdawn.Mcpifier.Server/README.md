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
mcpifier generate --swagger https://api.example.com/swagger.json --output path/to/mappings.json

# Edit mappings.json to customize:
# - Tool names and descriptions
# - Add/remove endpoints
# - Adjust parameter schemas

# Serve with your customized mappings
mcpifier serve --mode http --mappings path/to/mappings.json
```

See the [Usage](#usage) section below for a detailed description of available commands.

## Installation

### Installation as a .NET Tool

Install the Mcpifier server globally using the .NET CLI:

```bash
dotnet tool install -g Summerdawn.Mcpifier.Server
```

Or locally in a project:

```bash
dotnet tool install Summerdawn.Mcpifier.Server
```

The .NET tool version of the server requires the .NET 8.0 runtime or later.

### Installation as a Standalone Binary

You can download pre-built standalone binaries from Mcpifier's [GitHub Releases](https://github.com/summerdawn-ai/mcpifier/releases).

Supported platforms:

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

The pre-built binaries are standalone, AOT-compiled executables that do not require a separate .NET runtime installation.

### Running from Package with dnx

If you have the .NET 10 or later SDK installed, you can run the Mcpifier server directly from its package using `dotnet tool exec` or `dnx` without installing it as a global or local tool, for example:

```bash
dotnet tool exec Summerdawn.Mcpifier.Server --yes -- serve --mode http --swagger https://api.example.com/swagger.json

# Or simply
dnx Summerdawn.Mcpifier.Server --yes -- serve --mode http --swagger https://api.example.com/swagger.json
```

See the [dotnet tool exec documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-exec) for more information.

## Usage

The Mcpifier command-line server supports the following commands:

### mcpifier serve

The `serve` command starts Mcpifier as a server in HTTP or stdio mode.

**Usage:**

```bash
mcpifier serve --mode <http|stdio> [OPTIONS]

# Or simply (serve is default)
mcpifier --mode <http|stdio> [OPTIONS]
```

**Options:**

- `--mode`, `-m` (required): The server mode to use - `http` or `stdio`
- `--swagger` (optional): The path or URL of a Swagger/OpenAPI specification JSON file from which to generate tool mappings
- `--mappings` (optional): The path to a tool mappings JSON file from which to load tool mappings
- `--settings` (optional): The path to any additional configuration JSON file to load (can be specified multiple times)
- `--no-default-settings` (optional): Skip loading default settings

See the [Configuration](#configuration) and [Tool Mapping](#tool-mapping) sections for information about settings and mappings that the command-line server defines by default or loads automatically.

**Examples:**

```bash
# HTTP mode with Swagger specification
mcpifier serve --mode http --swagger https://api.example.com/swagger.json

# stdio mode with local Swagger specification
mcpifier serve --mode stdio --swagger path/to/swagger.json

# HTTP mode with custom settings and mappings
mcpifier serve --mode http --settings path/to/settings.json --mappings path/to/mappings.json

# Skip default settings and use only local appsettings.json and mappings.json
mcpifier serve --mode http --no-default-settings
```

### mcpifier generate

The `generate` command generates tool mappings from a Swagger/OpenAPI specification and saves them to a JSON file, then exits.

**Usage:**

```bash
mcpifier generate --swagger <file-or-url> [OPTIONS]
```

**Options:**

- `--swagger` (required): The path or URL of the Swagger/OpenAPI specification JSON file from which to generate tool mappings
- `--output`, `-o` (optional): The output path for the generated mapping file [default: mappings.json]
- `--settings` (optional): The path to any additional configuration JSON file to load (can be specified multiple times)
- `--no-default-settings` (optional): Skip loading default settings

See the [Configuration](#configuration) section for information about settings that the command-line server defines by default or loads automatically.

**Examples:**

```bash
# Generate from local file
mcpifier generate --swagger path/to/swagger.json

# Generate from URL
mcpifier generate --swagger https://api.example.com/swagger.json

# Custom output file
mcpifier generate --swagger path/to/swagger.json --output path/to/mappings.json
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

See the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#tool-mapping) for details on the tool mapping and the generated JSON file structure.

## Configuring MCP Clients

Mcpifier can be used with any MCP client that supports either stdio or HTTP transport modes. Below are configuration examples in the common JSON schema used by most clients - adapt to your specific client's configuration file format as needed.

In both modes, you can use the `DOTNET_CONTENTROOT` environment variable to point to a directory with custom `appsettings.json` and `mappings.json` files that the server will load automatically. This is explained in detail in the [Configuration](#configuration) section below.

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

Alternatively, you can use the .NET `dnx` tool to run the server directly from its package:

```json
{
  "servers": {
    "my-stdio-mcpifier": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Summerdawn.Mcpifier.Server", "--yes", "--", "serve", "--mode", "stdio", "--swagger", "path/to/swagger.json"],
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

Refer to the [Installation](#running-from-package-with-dnx) section for more information.

### Mcpifier in HTTP mode

To connect to a running Mcpifier command-line server in HTTP mode, add the following to your MCP client's configuration file:

```json
{
  "servers": {
    "my-http-mcpifier": {
      "type": "http",
      "url": "https://localhost:7025",
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config"
      }
    }
  }
}
```

By default, the Mcpifier command-line server is configured to listen on `https://0.0.0.0:7025` and `http://0.0.0.0:5157` in HTTP mode. You can change this by modifying the server configuration as explained in the [Configuration](#configuration) section.

## Authorization

Mcpifier supports multiple strategies for handling authorization when accessing REST APIs that require it. All of them can be configured by editing the server configuration as explained in the [Configuration](#configuration) section below.

### Static Authorization via Default Headers

Use the `DefaultHeaders` configuration setting to provide an authorization header that is included in every request. This works in stdio mode and HTTP mode:

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

Instead of using a configuration file, you can also [configure the setting using an environment variable](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider), for example:

```json
{
  "servers": {
    "my-http-mcpifier": {
      "type": "http",
      "url": "https://localhost:7025",
      "env": {
        "DOTNET_CONTENTROOT": "path/to/config",
        "MCPIFIER__REST__DEFAULTHEADERS__AUTHORIZATION": "Bearer 123...abc"
      }
    }
  }
}
```

### Client-Provided Authorization via Forwarded Headers

Some MCP clients support configuring request headers for HTTP mode MCP servers:

```json
{
  "servers": {
    "my-http-mcpifier": {
      "type": "http",
      "url": "https://localhost:5001",
      "headers": {
        "Authorization": "Bearer 123...abc"
      }
    }
  }
}
```

By default, the Mcpifier command-line server is configured to forward `Authorization` headers from the client to the REST API in HTTP mode. This can be changed by editing the server configuration.

### OAuth with MCP Authorization

If your MCP client supports the MCP Authorization protocol, you can enable it by editing the server configuration to allow the client to acquire and use an OAuth token, which Mcpifier will forward to the REST API:

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

For more details about authorization, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#authorization) for stdio mode, and in the [Mcpifier ASP.NET Core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.AspNetCore/README.md#authorization) for HTTP mode.

## Configuration

The Mcpifier command-line server uses [.NET Configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) to load configuration settings from multiple sources in a specific order, with later sources overriding earlier ones:

1. **Embedded default settings** - Built-in defaults compiled into the executable, unless skipped with `--no-default-settings`
2. **Local configuration file** - Local `appsettings.json` file in content directory, if present
3. **Environment variables** - System or process environment variables
4. **Command-line options** - Additional configuration files specified with the `--settings` option

By default, the content directory is the current working directory, but this can be overridden by setting the `DOTNET_CONTENTROOT` environment variable to point to a different directory.

### Default Settings

The Mcpifier command-line server includes embedded default settings that provide sensible defaults for most scenarios:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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
      "DefaultHeaders": {
        "User-Agent": "Mcpifier/1.0"
      },
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "ServerInfo": {
      "Name": "summerdawn-mcpifier",
      "Title": "Summerdawn Mcpifier MCP-to-REST Gateway",
      "Version": "1.0.0"
    },
    "Authorization": {
      "RequireAuthorization": false
    }
  }
}
```

If you want to skip, instead of override, these defaults, use the `--no-default-settings` option when starting the server. This is especially useful for clearing the `DefaultHeaders` and `ForwardedHeaders` settings.

### Customizing Settings

You can customize the configuration using any or all of the following methods:

#### Custom appsettings.json in Working or Content Directory

Place an `appsettings.json` file in the current working directory (or custom content directory). This file will be loaded after the embedded defaults and can override any settings:

```json
{
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "Authorization": "Bearer my-token",
        "X-Custom-Header": "value"
      },
      "ForwardedHeaders": {
        "Authorization": false
      }
    }
  }
}
```

#### Environment Variables

Configure settings via environment variables using the [.NET configuration provider syntax](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider). Use double underscores (`__`) to represent nested sections:

```bash
# Set the REST API base address
export MCPIFIER__REST__BASEADDRESS=https://api.example.com

# Add an authorization header
export MCPIFIER__REST__DEFAULTHEADERS__AUTHORIZATION="Bearer my-token"

# Change the HTTP port
export KESTREL__ENDPOINTS__HTTP__URL=http://0.0.0.0:8080
```

Environment variables are particularly useful when running Mcpifier in MCP clients:

```json
{
  "servers": {
    "my-mcpifier": {
      "type": "stdio",
      "command": "mcpifier",
      "args": ["--mode", "stdio", "--swagger", "https://api.example.com/swagger.json"],
      "env": {
        "MCPIFIER__REST__DEFAULTHEADERS__AUTHORIZATION": "Bearer my-token"
      }
    }
  }
}
```

#### Custom Settings Files via --settings

Use the `--settings` option to load custom configuration files. This option can be specified multiple times, with later files overriding earlier ones:

```bash
# Single custom settings file
mcpifier serve --mode http --settings custom.json

# Multiple settings files (dev.json overrides base.json)
mcpifier serve --mode http --settings base.json --settings dev.json

# Combine with Swagger
mcpifier serve --mode http --settings custom.json --swagger https://api.example.com/swagger.json
```

Custom settings files use the same JSON structure as `appsettings.json`.

#### Skip Default Settings with --no-default-settings

Use `--no-default-settings` to start with a blank configuration and only use settings you explicitly provide. This is useful when you want complete control or need to clear default headers:

```bash
# Clear default User-Agent and Authorization forwarding
mcpifier serve --mode http --settings clean.json --no-default-settings
```

Example `clean.json` that starts from scratch:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5157"
      }
    }
  },
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com"
    }
  }
}
```

### Configuration Examples

**Example 1: Change HTTP ports**

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      },
      "Https": {
        "Url": "https://0.0.0.0:8443"
      }
    }
  }
}
```

**Example 2: Add custom headers and base address**

```json
{
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "Authorization": "Bearer my-api-key",
        "X-API-Version": "2.0"
      }
    }
  }
}
```

**Example 3: Disable authorization forwarding**

```json
{
  "Mcpifier": {
    "Rest": {
      "ForwardedHeaders": {
        "Authorization": false
      }
    }
  }
}
```

**Example 4: Environment-specific configuration**

```bash
# base.json - shared settings
{
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com"
    }
  }
}

# dev.json - development overrides
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Summerdawn.Mcpifier": "Trace"
    }
  }
}

# Run with both
mcpifier serve --mode http --settings base.json --settings dev.json
```

### Configuration Reference

For the complete list of available settings and their descriptions, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#configuration).

## Tool Mapping

Tool mappings define how REST API endpoints are exposed as MCP tools. Mcpifier supports multiple ways to provide tool mappings:

1. **Local mappings file** - Local `mappings.json` file in content directory, if present
2. **Command-line mappings** - Additional mappings file specified with the `--mappings` option
3. **Command-line generation** - Tool mappings generated from a Swagger/OpenAPI specification JSON file specified with the `--swagger` option

The `--swagger` and `--mappings` options are mutually exclusive. You can use one or the other, not both.

By default, the content directory is the current working directory, but this can be overridden by setting the `DOTNET_CONTENTROOT` environment variable to point to a different directory.

### Tool Mapping Reference

The tool mapping configuration format and interpolation rules are documented in the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#tool-mapping).

## Logging

By default, the Mcpifier command-line server is configured to log:
- Startup configuration summary
- Tool count and names
- Each tool call (name, method, URL)
- Forwarded header names
- REST API response status codes

You can configure the logging level by editing the configuration as explained in the [Configuration](#configuration) section:

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

**Problem**: Error "No tool mappings have been configured"<br>
**Solution**: Ensure you provide tool mappings via one of these methods:
- Use `--swagger` to load from a Swagger/OpenAPI specification
- Use `--mappings` to load from a custom mappings file
- Place a `mappings.json` file in the working or content directory

**Problem**: Settings not taking effect<br>
**Solution**: Remember the configuration order - later sources override earlier ones:

1. Embedded default settings
2. Local configuration file
3. Environment variables
4. Command-line options

Use the `--no-default-settings` option if you need to completely override default settings.

### Connection Problems

**Problem**: Cannot connect to REST API<br>
**Solution**: Check the following:
- Verify that the correct `BaseAddress` is configured
- Verify network connectivity to the API
- Check firewall rules

**Problem**: Cannot connect to server in HTTP mode<br>
**Solution**: Connect to the default ports 5157 (HTTP) or 7025 (HTTPS), or change them in the configuration.

### Authentication Issues

**Problem**: 401/403 errors from REST API<br>
**Solution**:
- For HTTP mode: Ensure `Authorization` header is configured in `ForwardedHeaders` (enabled by default)
- For stdio mode: Use `DefaultHeaders` to include authorization
- Check that the specified credential is valid for your REST API

### Debug Logging

Enable detailed logging as explained in the [Logging](#logging) section:

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

## Resources

### Mcpifier

- [Mcpifier GitHub repository](https://github.com/summerdawn-ai/mcpifier)
- [Mcpifier releases and changelog](https://github.com/summerdawn-ai/mcpifier/releases)
- [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md)
- [Mcpifier ASP.NET Core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.AspNetCore/README.md)

### Model Context Protocol

- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/2025-06-18)
- [MCP Authorization documentation](https://modelcontextprotocol.io/docs/tutorials/security/authorization)

### .NET

- [dotnet CLI documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [.NET Configuration documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
 
## License

This project is licensed under the MIT License.
