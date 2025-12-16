# Summerdawn.Mcpify.AspNetCore

ASP.NET Core integration for hosting Mcpify MCP servers in web applications.

## Overview

This package provides seamless integration between Mcpify and ASP.NET Core, enabling you to:

- **Host MCP endpoints** alongside your existing API
- **Map MCP routes** using familiar ASP.NET Core patterns
- **Configure via appsettings.json** and dependency injection
- **Use standard middleware** for authentication, logging, etc.

This is the **recommended package for most users** who want to add MCP capabilities to their applications.

## Installation

```bash
dotnet add package Summerdawn.Mcpify.AspNetCore
```

## Dependencies

- This package automatically includes **Summerdawn.Mcpify**
- Requires **ASP.NET Core 8.0** or later

## Hosting Scenarios

### Scenario 1: Minimal Standalone Host

Create a minimal MCP server that proxies to an external REST API:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load tool mappings from separate file
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpify services
builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"))
    .AddAspNetCore();

var app = builder.Build();

app.UseRouting();

// Map Mcpify to root path
app.MapMcpify();

app.Run();
```

**mappings.json:**
```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com"
    },
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

### Scenario 2: Alongside Your REST API

Host Mcpify in the same application as the REST API it proxies:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add your API services
builder.Services.AddControllers();

// Load Mcpify tool mappings
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpify - configure base address to "/" since API is in same host
builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"))
    .AddAspNetCore();

builder.Services.Configure<McpifyOptions>(options =>
{
    options.Rest.BaseAddress = "/"; // Proxy to self
});

var app = builder.Build();

app.UseRouting();

// Map both your REST API and Mcpify
app.MapControllers();        // Your REST API endpoints
app.MapMcpify("/mcp");       // Mcpify at /mcp route

app.Run();
```

This enables AI assistants to call your API through MCP while keeping your REST API accessible directly.

### Scenario 3: Separate Host with Custom Route

Run Mcpify on a different port or path with custom configuration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("mappings.json");

builder.Services.AddMcpify(builder.Configuration.GetSection("Mcpify"))
    .AddAspNetCore();

// Custom Kestrel configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // HTTP
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var app = builder.Build();

app.UseRouting();
app.MapMcpify("/api/mcp"); // Custom route

app.Run();
```

## Configuration

### Auth Header Forwarding

Forward authentication headers from MCP clients to your REST API:

**appsettings.json:**
```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "ForwardedHeaders": {
        "Authorization": true,
        "X-Custom-Header": true,
        "X-API-Key": true
      }
    }
  }
}
```

When an MCP client sends a request with these headers, Mcpify forwards them to the REST API.

### Default Headers

Set headers that are always sent to the REST API:

```json
{
  "Mcpify": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "User-Agent": "MyApp-MCP/1.0",
        "X-API-Key": "your-api-key-here",
        "Accept": "application/json"
      }
    }
  }
}
```

**Key Difference:**
- **Forwarded headers**: Come from the MCP client's request (dynamic per request)
- **Default headers**: Always sent with every REST API request (static)

### Tool Mappings

Define tools in configuration (recommended: separate file):

```json
{
  "Mcpify": {
    "Tools": [
      {
        "mcp": {
          "name": "create_item",
          "description": "Create a new item",
          "inputSchema": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "value": { "type": "number" }
            },
            "required": ["name"]
          }
        },
        "rest": {
          "method": "POST",
          "path": "/items",
          "body": "{ \"name\": {name}, \"value\": {value} }"
        }
      }
    ]
  }
}
```

For detailed configuration options, see the [main README](https://github.com/summerdawn-ai/mcpify/blob/main/README.md#configuration).

## Advanced Configuration

### Authorization Requirements

Require authorization for MCP endpoints:

```csharp
builder.Services.Configure<McpifyOptions>(options =>
{
    options.Authentication.RequireAuthorization = true;
});

app.UseAuthentication();
app.UseAuthorization();
app.MapMcpify().RequireAuthorization(); // Apply auth policy
```

### Custom Routes

Map Mcpify to any route:

```csharp
// Root
app.MapMcpify();

// Custom path
app.MapMcpify("/mcp");

// With route prefix
app.MapMcpify("/api/v1/mcp");
```

### CORS Configuration

Enable CORS if needed:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMcpClients", policy =>
    {
        policy.WithOrigins("https://trusted-client.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowMcpClients");
app.MapMcpify();
```

## Standalone Server

If you don't want to write any code, a pre-built standalone server is available:

```bash
dotnet tool install -g Summerdawn.Mcpify.Server
mcpify-server --mode http
```

Or download from [GitHub Releases](https://github.com/summerdawn-ai/mcpify/releases).

## Further Documentation

- **Tool Mappings**: See the [main README](https://github.com/summerdawn-ai/mcpify/blob/main/README.md#configuration) for detailed mapping configuration
- **Standalone Server**: See [Summerdawn.Mcpify.Server README](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify.Server/README.md)
- **Core Library**: See [Summerdawn.Mcpify README](https://github.com/summerdawn-ai/mcpify/blob/main/src/Summerdawn.Mcpify/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpify](https://github.com/summerdawn-ai/mcpify)

## License

This project is licensed under the MIT License.
