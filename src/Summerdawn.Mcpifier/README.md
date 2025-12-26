# Summerdawn.Mcpifier

Mcpifier is a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON or generate them automatically from Swagger/OpenAPI specifications, and Mcpifier translates requests between MCP clients and your REST service.

This package provides the foundational components for building gateways from stdio MCP clients to REST APIs:

- **JSON-RPC 2.0 message handling** - Parse and dispatch MCP protocol messages
- **MCP protocol implementation** - Server info, tool listing, and tool execution
- **REST API service** - HTTP client with parameter interpolation
- **STDIO support** - Process-based communication via stdio
- **Swagger/OpenAPI integration** - Automatic tool generation from API specifications

### When to Use This Package

Use `Summerdawn.Mcpifier` when you need:
- Fine-grained control over MCP message handling
- Custom hosting scenarios beyond ASP.NET Core
- Integration with existing service architectures
- STDIO-based communication for process integration

**Most users should use `Summerdawn.Mcpifier.AspNetCore` instead**, which provides HTTP support using ASP.NET Core.

## Quickstart

### With Swagger/OpenAPI (Recommended)

The fastest way to get started is to generate tools from your existing Swagger/OpenAPI specification:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add Mcpifier with automatic tool generation from Swagger
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("https://api.example.com/swagger.json");

// Send console logging to stderr for stdio mode
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var app = builder.Build();
app.UseMcpifier();
app.Run();
```

**Minimal appsettings.json:**
```json
{
  "Mcpifier": {
    "Rest": {
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    "ServerInfo": {
      "Name": "my-mcp-server"
    }
  }
}
```

Note: `BaseAddress` is automatically extracted from the Swagger specification.

### With Manual Configuration

If you prefer manual configuration or need full control:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Load tool mappings from file
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpifier services
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var app = builder.Build();
app.UseMcpifier();
app.Run();
```

For complete configuration documentation including authorization, header forwarding, and all available settings, see the [main README](https://github.com/summerdawn-ai/mcpifier#configuration).

## Installation

```bash
dotnet add package Summerdawn.Mcpifier
```

## Configuration

For detailed configuration including: 
- All configuration settings and their meanings
- Tool mappings structure
- Parameter interpolation
- Authorization scenarios (MCP Authorization, default headers, forwarded headers)
- Loading from separate files
- Generating from OpenAPI specs

See the [main README Configuration section](https://github.com/summerdawn-ai/mcpifier#configuration).

## Dependencies

- **.NET 8.0** or later
- **Microsoft.Extensions.Http** - HTTP client factory
- **Microsoft.Extensions.Hosting.Abstractions** - Hosting abstractions
- **Microsoft.AspNetCore.Http.Abstractions** - HTTP context abstractions
- **Microsoft.OpenApi.Readers** - OpenAPI/Swagger specification parsing

## Usage

### Service Registration

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

// In your service configuration
services.AddMcpifier(configuration.GetSection("Mcpifier"));
```

### AddToolsFromSwagger - Core Functionality

The `AddToolsFromSwagger` method enables automatic tool generation from OpenAPI/Swagger specifications:

```csharp
// Basic usage - load from file or URL
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json");

// Load from URL
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("https://api.example.com/swagger.json");
```

### Filtering Tools

Filter out unwanted endpoints using a predicate:

```csharp
// Exclude all /users endpoints
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/users"));

// Include only specific HTTP methods
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => mapping.Rest.Method is "GET" or "POST");

// Complex filtering
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => 
            !mapping.Rest.Path.StartsWith("/internal") &&
            !mapping.Rest.Path.StartsWith("/admin"));
```

### Customizing Mappings

Use an action to modify mappings before they're registered:

```csharp
// Add prefix to all tool names
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Name = "api_" + mapping.Mcp.Name;
        });

// Modify descriptions
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Description = $"[External API] {mapping.Mcp.Description}";
        });

// Combined filter and action
builder.Services.AddMcpifier(configuration.GetSection("Mcpifier"))
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping => mapping.Mcp.Name = "myapi_" + mapping.Mcp.Name,
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));
```

### Customization Workflow

For complete control, the `SwaggerConverter` can be used to load tools into configuration or save to a file:

```csharp
// Get the converter from DI
var converter = serviceProvider.GetRequiredService<SwaggerConverter>();

// Load and convert
var mappings = await converter.LoadAndConvertAsync("swagger.json");

// Save to file for later customization
await converter.LoadAndConvertAsync("swagger.json", "mappings.json");

// Now you can manually edit mappings.json, then load it:
builder.Configuration.AddJsonFile("mappings.json");
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));
```

This workflow enables:
1. Generate mappings from Swagger
2. Save to `mappings.json`
3. Manually customize as needed
4. Load and serve from the customized file

### Basic Configuration Example

```json
{
  "Mcpifier": {
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

For complete configuration documentation including authorization, header forwarding, and all available settings, see the [main README](https://github.com/summerdawn-ai/mcpifier#configuration).

### Using with ASP.NET Core

If you're building an ASP.NET Core application, use the `Summerdawn.Mcpifier.AspNetCore` package instead:

```bash
dotnet add package Summerdawn.Mcpifier.AspNetCore
```

It provides simpler integration with endpoint routing and middleware.

## API Overview

### Key Classes and Services

**RestApiService**
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

- **Configuration Details**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuration)
- **MCP Client Setup**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuring-mcp-clients-for-mcpifier)
- **ASP.NET Core Integration**: [Summerdawn.Mcpifier.AspNetCore](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.AspNetCore/README.md)
- **Standalone Server**: [Summerdawn.Mcpifier.Server](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.Server/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpifier](https://github.com/summerdawn-ai/mcpifier)

## License

This project is licensed under the MIT License.
