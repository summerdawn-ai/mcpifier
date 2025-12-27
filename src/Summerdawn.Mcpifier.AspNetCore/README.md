# Summerdawn.Mcpifier.AspNetCore

ASP.NET Core integration and HTTP mode for Mcpifier - a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier can be used as a library, ASP.NET Core middleware, or a command-line server and tool. It supports automatic tool generation from Swagger/OpenAPI specifications using conventions that map REST endpoints to MCP tools, or full customization using JSON configuration files.

This package provides integration with ASP.NET Core so you can run Mcpifier in HTTP mode alongside your own controllers or as a separate web application.

Other available packages:

- [Summerdawn.Mcpifier](https://www.nuget.org/packages/Summerdawn.Mcpifier): Core package with MCP implementation and stdio server
- [Summerdawn.Mcpifier.Server](https://www.nuget.org/packages/Summerdawn.Mcpifier.Server): Command-line server and tool

## Getting Started

The fastest way to get started is to let Mcpifier generate tools from an existing Swagger/OpenAPI specification:

```csharp
using Microsoft.AspNetCore.Builder;

using Summerdawn.Mcpifier.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Mcpifier with automatic tool generation from Swagger/OpenAPI specification
builder.Services
    .AddMcpifier(options =>
    {
        options.Rest.ForwardedHeaders = new() { ["Authorization"] = true };
        options.ServerInfo = new() { Name = "my-mcp-server" };
    })
    .AddAspNetCore()
    .AddToolsFromSwagger("https://api.example.com/swagger.json");

// Configure CORS as needed
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

// Enable Mcpifier HTTP(S) endpoints
app.MapMcpifier();

app.Run();
```

When a Swagger/OpenAPI document is loaded, the REST API base address is inferred from the specification unless already configured.

See the [Usage](#usage) section below for a detailed description of available features and customization options.

## Installation

Install Mcpifier with ASP.NET Core integration via the .NET CLI:

```bash
dotnet add package Summerdawn.Mcpifier.AspNetCore
```

## Usage

### Service Registration

Call `AddMcpifier(...).AddAspNetCore()` on an [ASP.NET Core WebApplication](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication) builder to register and configure Mcpifier services and endpoints. You can either provide an `IConfiguration` section from a configuration manager, or an action to configure the `McpifierOptions` instance directly:

```csharp
using Microsoft.AspNetCore.Builder;

using Summerdawn.Mcpifier.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings etc.
builder.Services
    .AddMcpifier(builder.Configuration.GetSection("Mcpifier"))
    .AddAspNetCore();

// Or configure directly
builder.Services
    .AddMcpifier(options =>
    {
        options.Rest.BaseAddress = "https://api.example.com";
        options.Rest.ForwardedHeaders = new() { ["Authorization"] = true };
        options.ServerInfo = new() { Name = "my-mcp-server" };
    })
    .AddAspNetCore();

// Configure CORS as needed
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
```

Because HTTP mode integrates with ASP.NET Core's request pipeline, you can layer in middleware (`UseRouting`, `UseCors`, `UseAuthentication`, etc.) exactly as you would for any other web app.

For a full reference of all Mcpifier configuration settings, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#configuration).

### Generating Tools from Swagger

Call `AddToolsFromSwagger` with a Swagger/OpenAPI specification file name or URL to automatically generate tool mappings when the application is started:

```csharp
// Load from file
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json");

// Load from URL
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("https://api.example.com/swagger.json");
```

Tool mappings can be modified as needed as described in the section [Adding Tools Manually](#adding-tools-manually). For the documentation of the generated tool mapping format, refer to the the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#tool-mapping).

Loading a Swagger/OpenAPI specification will also **set the REST API base address** to the base URL specified in the specification, or the base address of the specification URL, if the Mcpifier configuration does not already specify a base address.

#### Filtering Tools

Filter out unwanted tool mappings using any predicate:

```csharp
// Exclude all /internal endpoints
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));

// Include only specific HTTP methods
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => mapping.Rest.Method is "GET" or "POST");
```

#### Customizing Tools

Specify an action to modify tool mappings before they're registered:

```csharp
// Add prefix to all tool names
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Name = "on_my_api_" + mapping.Mcp.Name;
        });

// Modify descriptions
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Description = $"[External API] {mapping.Mcp.Description}";
        });

// Combine filter and action
builder.Services.AddMcpifier(options => { /* configure */ }).AddAspNetCore()
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping => mapping.Mcp.Name = "on_my_api_" + mapping.Mcp.Name,
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));
```

### Adding Tools Manually

For complete control, you can generate tool mappings once using the `SwaggerConverter` class or the Mcpifier CLI and modify the resulting `mappings.json` file as needed:

```csharp
using Microsoft.AspNetCore.Builder;

using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Services;

// Build a temporary host for one-time mappings generation
var builder = WebApplication.CreateBuilder(args);

// Register Swagger converter
builder.Services.AddMcpifier(_ => { }).AddAspNetCore();

// Get Swagger converter from DI
var swaggerConverter = builder.Build().Services.GetRequiredService<SwaggerConverter>(); 

// Generate mappings from specification and save to file
await swaggerConverter.LoadAndConvertAsync("https://api.example.com/swagger.json", "path/to/mappings.json");

// Exit
```

This is intended for offline or one-time generation scenarios.

After modifying the generated `mappings.json` file as needed (e.g. changing tool descriptions and names, removing mappings), you can load tool mappings directly from the file:

```csharp
using Microsoft.AspNetCore.Builder;

using Summerdawn.Mcpifier.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load the mappings file into the configuration
builder.Configuration.AddJsonFile("path/to/mappings.json");

// Configure Mcpifier with the resulting configuration including the tool mappings
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier")).AddAspNetCore();
```

### Starting the Server

After configuration is complete, call `MapMcpifier` on the web app host to enable Mcpifier HTTP endpoints.

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

// Enable Mcpifier HTTP(S) endpoints
app.MapMcpifier("/mcp");

app.Run();
```

`MapMcpifier` can coexist with `MapControllers`, minimal APIs, or SignalR hubs, and it respects any middleware placed before it.

## Authorization

Mcpifier in HTTP mode supports multiple strategies for handling authorization when accessing REST APIs that require it.

### Static Authorization via Default Headers

Use the `DefaultHeaders` configuration setting to provide an authorization header that is included in every request:

```jsonc
{
  "Mcpifier": {
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": {
        // API key
        "X-API-Key": "your-api-key-here",
        // Or bearer token
        "Authorization": "Bearer 123...abc"
      }
    }
  }
}
```

Instead of configuring this setting in `appsettings.json`, you can also [specify it as an environment variable](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider), for example:

```powershell
# PowerShell
$env:MCPIFIER__REST__DEFAULTHEADERS__AUTHORIZATION="Bearer 123...abc"
```

### Client-Provided Authorization via Forwarded Headers

Instead of static authorization headers, you can configure Mcpifier to forward client-provided headers using the `ForwardedHeaders` configuration setting:

```jsonc
{
  "Mcpifier": {
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

This delegates authentication entirely to the MCP client by providing the REST API with whatever authorization header the client supplied, if any.

### OAuth with MCP Authorization

Mcpifier also supports [MCP Authorization](https://modelcontextprotocol.io/docs/tutorials/security/authorization):

```jsonc
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
        "AuthorizationServers": ["https://auth.example.com/oauth/v2.0"],
        "ScopesSupported": ["https://mcp.example.com/access"]
      }
    }
  }
}
```

When enabled, any MCP requests (not just tool calls!) without an `Authorization` header receive an `HTTP 401 Unauthorized` response with a `WWW-Authenticate` header pointing to the configured `ResourceMetadata`. This allows the client to acquire an OAuth token from the configured authorization server and include it in future calls to Mcpifier, which will then forward the token to the REST API.

## Configuration

For the full list of settings, refer to the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#configuration).

## Tool Mapping

Tool mapping configuration settings and interpolation rules are documented in the corresponding section in the [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md#tool-mapping).

## Dependencies

- **.NET 8.0** or later
- **Microsoft.AspNetCore.App** framework reference for ASP.NET Core integration
- **Summerdawn.Mcpifier** for core services

## Resources

- [Mcpifier GitHub repository](https://github.com/summerdawn-ai/mcpifier)
- [Mcpifier core documentation](https://github.com/summerdawn-ai/mcpifier/blob/main/src/Summerdawn.Mcpifier/README.md)
- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/2025-06-18)
- [MCP Authorization](https://modelcontextprotocol.io/docs/tutorials/security/authorization)
- [OAuth 2.0 Protected Resource Metadata](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-resource-metadata)

## License

This project is licensed under the MIT License.
