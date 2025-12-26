# Summerdawn.Mcpifier.AspNetCore

ASP.NET Core integration for Mcpifier. Mcpifier is a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier enables you to expose REST APIs as MCP tools without writing any code. Simply configure your API endpoint mappings in JSON or generate them automatically from Swagger/OpenAPI specifications, and Mcpifier translates requests between MCP clients and your REST service.

This package provides seamless integration between Mcpifier and ASP.NET Core, enabling you to:

- **Host MCP endpoints** alongside your existing API
- **Map MCP routes** using familiar ASP.NET Core patterns
- **Use standard middleware** for authentication, logging, etc.
- **Generate tools from Swagger** - Automatic tool creation from OpenAPI specifications

This is the **recommended package for most users** who want to add MCP capabilities to their applications.

## Quickstart

### With Swagger/OpenAPI (Recommended)

The fastest way to get started is to generate tools from your existing Swagger/OpenAPI specification:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Mcpifier with automatic tool generation from Swagger
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("https://api.example.com/swagger.json");

var app = builder.Build();

app.UseRouting();

// Map Mcpifier to root path
app.MapMcpifier();

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
      "Name": "my-api-server"
    }
  }
}
```

Note: `BaseAddress` is automatically extracted from the Swagger specification.

### With Manual Configuration

If you prefer manual configuration or need full control:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load tool mappings from separate file
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpifier services
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore();

var app = builder.Build();

app.UseRouting();

// Map Mcpifier to root path
app.MapMcpifier();

app.Run();
```

## Installation

```bash
dotnet add package Summerdawn.Mcpifier.AspNetCore
```

## Configuration

This package supports all Mcpifier configuration options.  For complete documentation including: 

- All configuration settings (`BaseAddress`, `DefaultHeaders`, `ForwardedHeaders`, etc.)
- Authorization scenarios (MCP Authorization, static headers, forwarded headers)
- Tool mappings structure
- Parameter interpolation

See the [main README Configuration section](https://github.com/summerdawn-ai/mcpifier#configuration).

### Quick Configuration Examples

**Static API Key:**
```json
{
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        "X-API-Key": "your-api-key-here"
      }
    }
  }
}
```

**OAuth with Header Forwarding:**
```json
{
  "Mcpifier": {
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

## Dependencies

- This package automatically includes **Summerdawn.Mcpifier**
- Requires **ASP.NET Core 8.0** or later

## Usage

### AddToolsFromSwagger - Core Functionality

The `AddToolsFromSwagger` method enables automatic tool generation from OpenAPI/Swagger specifications:

```csharp
// Basic usage - load from file or URL
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json");

// Load from URL
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("https://api.example.com/swagger.json");
```

### Filtering Tools

Filter out unwanted endpoints using a predicate:

```csharp
// Exclude all /users endpoints
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/users"));

// Include only specific HTTP methods
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => mapping.Rest.Method is "GET" or "POST");

// Complex filtering - easily exclude internal/admin endpoints
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => 
            !mapping.Rest.Path.StartsWith("/internal") &&
            !mapping.Rest.Path.StartsWith("/admin"));
```

### Customizing Mappings

Use an action to modify mappings before they're registered:

```csharp
// Add prefix to all tool names
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Name = "api_" + mapping.Mcp.Name;
        });

// Combined filter and action
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping => mapping.Mcp.Name = "myapi_" + mapping.Mcp.Name,
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));
```

### Customization Workflow

For complete control, the `SwaggerConverter` can be used to load tools into configuration or save to a file:

```csharp
// During app configuration, generate and save mappings
var converter = app.Services.GetRequiredService<SwaggerConverter>();
await converter.LoadAndConvertAsync("swagger.json", "mappings.json");

// Now manually edit mappings.json, then load it:
builder.Configuration.AddJsonFile("mappings.json");
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore();
```

This workflow enables:
1. Generate mappings from Swagger
2. Save to `mappings.json`
3. Manually customize as needed (tool names, descriptions, add/remove endpoints, adjust schemas)
4. Load and serve from the customized file

### Scenario 1: Minimal Standalone Host

Create a minimal MCP server that proxies to an external REST API:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Load tool mappings from separate file
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpifier services
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore();

var app = builder.Build();

app.UseRouting();

// Map Mcpifier to root path
app.MapMcpifier();

app.Run();
```

**mappings.json:**
```json
{
  "Mcpifier": {
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

Host Mcpifier in the same application as the REST API it proxies:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add your API services
builder.Services.AddControllers();

// Load Mcpifier tool mappings
builder.Configuration.AddJsonFile("mappings.json");

// Add Mcpifier - configure base address to "/" since API is in same host
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore();

builder.Services.Configure<McpifierOptions>(options =>
{
    options.Rest.BaseAddress = "/"; // Forward to self
});

var app = builder.Build();

app.UseRouting();

// Map both your REST API and Mcpifier
app.MapControllers();        // Your REST API endpoints
app.MapMcpifier("/mcp");       // Mcpifier at /mcp route

app.Run();
```

This enables AI assistants to call your API through MCP while keeping your REST API accessible directly.

### Scenario 3: Separate Host with Custom Route

Run Mcpifier on a different port or path with custom configuration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("mappings.json");

builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
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
app.MapMcpifier("/api/mcp"); // Custom route

app.Run();
```

## Advanced Configuration

### Authorization Requirements

Require authorization for MCP endpoints:

```csharp
builder.Services.Configure<McpifierOptions>(options =>
{
    options.Authorization.RequireAuthorization = true;
    options.Authorization.ResourceMetadata = new Dictionary<string, object>
    {
        ["Resource"] = "https://mcp.example.com",
        ["AuthorizationServers"] = new[] { "https://auth.example.com/oauth/v2.0" },
        ["ScopesSupported"] = new[] { "https://mcp.example.com/access" }
    };
});

app.MapMcpifier();
```

See the [main README Authorization section](https://github.com/summerdawn-ai/mcpifier#authorization-scenarios) for complete details.

### Custom Routes

Map Mcpifier to any route:

```csharp
// Root
app.MapMcpifier();

// Custom path
app.MapMcpifier("/mcp");

// With route prefix
app.MapMcpifier("/api/v1/mcp");
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
app.MapMcpifier();
```

## Further Documentation

- **Configuration Details**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuration)
- **MCP Client Setup**: [Main README](https://github.com/summerdawn-ai/mcpifier#configuring-mcp-clients-for-mcpifier)
- **Core Library**: [Summerdawn.Mcpifier](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md)
- **Standalone Server**: [Summerdawn.Mcpifier.Server](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier.Server/README.md)
- **GitHub Repository**: [summerdawn-ai/mcpifier](https://github.com/summerdawn-ai/mcpifier)

## License

This project is licensed under the MIT License.
