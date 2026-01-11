# Summerdawn.Mcpifier

Mcpifier is a zero-code MCP (Model Context Protocol) gateway that exposes an existing REST API as an MCP server.

## Overview

Mcpifier can be used as a library, ASP.NET Core middleware, or a command-line server and tool. It supports automatic tool generation from Swagger/OpenAPI specifications using conventions that map REST endpoints to MCP tools, or full customization using JSON configuration files.

This is the core package providing MCP protocol implementation, a Swagger/OpenAPI converter, REST API service, and stdio server.

Other available packages:

- [Summerdawn.Mcpifier.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpifier.AspNetCore): ASP.NET Core integration and HTTP server
- [Summerdawn.Mcpifier.Server](https://www.nuget.org/packages/Summerdawn.Mcpifier.Server): Command-line server and tool

## Getting Started

The fastest way to get started is to let Mcpifier generate tools from an existing Swagger/OpenAPI specification:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add Mcpifier with automatic tool generation from Swagger/OpenAPI specification
builder.Services
    .AddMcpifier(options =>
    {
        options.Rest.DefaultHeaders = new() { ["Authorization"] = "Bearer 123...abc" };
        options.ServerInfo = new() { Name = "my-mcp-server" };
    })
    .AddToolsFromSwagger("https://api.example.com/swagger.json");

// Send console logging to stderr for stdio mode
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var app = builder.Build();

// Enable Mcpifier stdio server
app.UseMcpifier();

app.Run();
```

When a Swagger/OpenAPI document is loaded, the REST API base address is inferred from the specification unless already configured.

See the [Usage](#usage) section below for a detailed description of available features and customization options.

## Installation

Install Mcpifier using the .NET CLI:

```bash
dotnet add package Summerdawn.Mcpifier
```

## Usage

### Service Registration

Call `AddMcpifier` on a [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) builder to register and configure Mcpifier services. You can either provide an `IConfiguration` section from a configuration manager, or an action to configure the `McpifierOptions` instance directly:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration from appsettings etc.
builder.Services.AddMcpifier(builder.Configuration.GetSection("Mcpifier"));

// Or configure directly
builder.Services
    .AddMcpifier(options =>
    {
        options.Rest.BaseAddress = "https://api.example.com";
        options.Rest.DefaultHeaders = new() { ["Authorization"] = "Bearer 123...abc" };
        options.ServerInfo = new() { Name = "my-mcp-server" };
        // etc.
    });
```

Make sure that logging statements are not sent to the console standard output when running in stdio mode:

```csharp
// Send console logging to stderr for stdio mode
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

See the [Configuration](#configuration) section for a full reference of all Mcpifier configuration settings.

### Generating Tools from Swagger

Call `AddToolsFromSwagger` with a Swagger/OpenAPI specification file name or URL to automatically generate tool mappings when the application is started:

```csharp
// Load from file
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json");

// Load from URL
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("https://api.example.com/swagger.json");
```

Tool mappings are generated in the format documented in the section [Tool Mapping](#tool-mapping), and can be modified as needed as described in the section [Adding Tools Manually](#adding-tools-manually).

Loading a Swagger/OpenAPI specification will also **set the REST API base address** to the base URL specified in the specification, or the base address of the specification URL, if the Mcpifier configuration does not already specify a base address.

#### Filtering Tools

Filter out unwanted tool mappings using any predicate:

```csharp
// Exclude all /internal endpoints
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));

// Include only specific HTTP methods
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json",
        mappingPredicate: mapping => mapping.Rest.Method is "GET" or "POST");
```

#### Customizing Tools

Specify an action to modify tool mappings before they're registered:

```csharp
// Add prefix to all tool names
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Name = "on_my_api_" + mapping.Mcp.Name;
        });

// Modify descriptions
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping =>
        {
            mapping.Mcp.Description = $"[External API] {mapping.Mcp.Description}";
        });

// Combine filter and action
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromSwagger("swagger.json",
        mappingAction: mapping => mapping.Mcp.Name = "on_my_api_" + mapping.Mcp.Name,
        mappingPredicate: mapping => !mapping.Rest.Path.StartsWith("/internal"));
```

### Adding Tools Manually

For complete control, you can generate tool mappings once using the `SwaggerConverter` class or the Mcpifier command-line server and modify the resulting `mappings.json` file as needed:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;
using Summerdawn.Mcpifier.Services;

// Build a temporary host for one-time mappings generation
var builder = Host.CreateApplicationBuilder(args);

// Register Swagger converter
builder.Services.AddMcpifier(_ => { });

// Get Swagger converter from DI
var swaggerConverter = builder.Build().Services.GetRequiredService<SwaggerConverter>(); 

// Generate mappings from specification and save to file
await swaggerConverter.LoadAndConvertAsync("https://api.example.com/swagger.json", "path/to/mappings.json");

// Exit
```

This is intended for offline or one-time generation scenarios.

After modifying the generated `mappings.json` file as needed (e.g. changing tool descriptions and names, removing mappings), you can load tool mappings directly from the file by calling `AddToolsFromMappings`:

```csharp
using Summerdawn.Mcpifier.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Load tool mappings from file
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromMappings("path/to/mappings.json");
```

As with Swagger/OpenAPI, loading tool mappings will also **set the REST API base address** to the base address specified in the file, if the Mcpifier configuration does not already specify a base address.

### Starting the Server

After configuration is complete, call `UseMcpifier` on the app host to enable the Mcpifier stdio server to run as a background service.

```csharp
var app = builder.Build();

// Enable Mcpifier stdio server
app.UseMcpifier();

app.Run();
```

## Authorization

MCP Authorization does not apply to stdio transport. In order to use Mcpifier in stdio mode with a REST API that requires authorization, you can use the `DefaultHeaders` configuration setting to provide an authorization header that is included in every request:

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

## Configuration

Mcpifier's configuration maps to the `McpifierOptions` class and is structured as follows:

```jsonc
{
  "Mcpifier": {
    // Rest API configuration
    "Rest": {
      "BaseAddress": "https://api.example.com",
      "DefaultHeaders": { 
          "User-Agent": "Mcpifier/1.0" 
      },
      // Forwarded headers (only in HTTP mode)
      "ForwardedHeaders": {
        "Authorization": true
      }
    },
    // MCP server configuration
    "ProtocolVersion": "2025-06-18",
    "ServerInfo": {
      "Name": "my-mcp-server",
      "Title": "My MCP Server",
      "Version": "1.0.0"
    },
    "Instructions": "",
    // MCP Authorization configuration (only in HTTP mode)
    "Authorization": {
      "RequireAuthorization": true,
      "ResourceMetadata": {
        "Resource": "https://mcp.example.com",
        "AuthorizationServers": [ "https://auth.example.com/oauth" ],
        "ScopesSupported": [ "https://mcp.example.com/access" ]
      }
    },
    // Mappings from MCP tools to REST API calls - loaded from separate JSON file
    "Tools": [
      {
        // MCP tool definition
        "mcp": {
          "name": "get_user",
          "description": "Retrieve user information by id",
          // JSON Schema
          "inputSchema": {
            "type": "object",
            "properties": {
              "userId": {
                "type": "string",
                "description": "The user's unique identifier"
              }
            },
            "required": ["userId"]
          }
        },
        // REST endpoint definition
        "rest": {
          "method": "GET",
          "path": "/users/{userId}",
          "query": "include=profile",
          "body": null
        }
      }
    ]
  }
}
```

Because tool mappings support full the JSON schema syntax, they can not be bound using the configuration manager and instead must be loaded using `AddToolsFromMappings`:

```csharp
builder.Services.AddMcpifier(options => { /* configure */ })
    .AddToolsFromMappings("path/to/mappings.json")
```

See the [Usage](#usage) section for instructions on how to load the configuration.

### Configuration Reference

#### Rest

The settings in the `Rest` configuration section are related to the REST API:

|Name|Type|Description|Example|
|----|----|-----------|-------|
|BaseAddress|`string` (required)|The Base URL for the target REST API - all REST API requests will be made relative to this URL|"https://api.example.com",<br>"/api" (only in HTTP mode)|
|DefaultHeaders|`Dictionary<string,string>`|Headers to include in every REST API request|{ "User-Agent": "Mcpifier/1.0" }|
|ForwardedHeaders|`Dictionary<string,bool>`|Headers to forward from the MCP tool call (only in HTTP mode)|{ "Authorization": true }|

The `BaseAddress` may be [set from a loaded Swagger/OpenAPI specification](#generating-tools-from-swagger) or [set from a mappings file](#adding-tools-manually), if available and not already specified in the configuration.

It can be set to a relative URL if Mcpifier is used in HTTP mode (using [Summerdawn.Mcpifier.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpifier.AspNetCore)) alongside REST endpoints hosted in the same ASP.NET Core application. In stdio mode, it must be an absolute URL.

`ForwardedHeaders` are only relevant in HTTP mode. In stdio mode, there are no headers to forward from the client.

#### ProtocolVersion, ServerInfo, Instructions

These settings configure the response of Mcpifier to an [MCP `initialize` request](https://modelcontextprotocol.io/specification/2025-06-18/basic/lifecycle):

|Name|Type|Description|Example|
|----|----|-----------|-------|
|ProtocolVersion|`string`|The MCP protocol version, if not default|"2025-06-18"|
|ServerInfo|`McpServerInfo`|The `serverInfo` section of the response|{ "Name": "my-mcp-server", "Title": "My MCP Server", "Version": "1.0.0" }|
|Instructions|`string`|Optional instructions for the client`|"Here's how to use this ..."|

#### Authorization

The settings in the `Authorization` section cover the implementation of the [MCP Authorization](https://modelcontextprotocol.io/docs/tutorials/security/authorization) specification:

|Name|Type|Description|Example|
|----|----|-----------|-------|
|RequireAuthorization|`bool`|Enable MCP Authorization flow for any unauthorized requests|`true`|
|ResourceMetadata|`ProtectedResourceMetadata`|The OAuth 2.0 Protected Resource Metadata to serve at `/.well-known/oauth-protected-resource`|{ "Resource": "https://mcp.example.com", "AuthorizationServers": [ "https://auth.example.com/oauth" ], "ScopesSupported": [ "https://mcp.example.com/access" ] }|

These settings are only relevant in HTTP mode using [Summerdawn.Mcpifier.AspNetCore](https://www.nuget.org/packages/Summerdawn.Mcpifier.AspNetCore). MCP Authorization does not support stdio mode because there are no response headers to send to the client.

#### Tools

The `Tools` section defines tool mappings that are loaded from configuration rather than generated dynamically from Swagger/OpenAPI.

See [Tool Mapping](#tool-mapping) below for a complete specification.

## Tool Mapping

Whether tool mappings are generated from a Swagger/OpenAPI specification or loaded from configuration, the result is an array of `McpifierToolMapping` instances, each of which maps an MCP tool definition to a REST API endpoint with the following structure:

```jsonc
{
  // MCP tool definition
  "mcp": {
    "name": "get_user",
    "description": "Retrieve user information by id",
    // JSON Schema
    "inputSchema": {
      "type": "object",
      "properties": {
        "userId": {
          "type": "string",
          "description": "The user's unique identifier"
        }
      },
      "required": ["userId"]
    }
  },
  // REST endpoint definition
  "rest": {
    "method": "GET",
    "path": "/users/{userId}",
    "query": "include=profile",
    "body": null
  }
}
```

### Tool Mapping Reference

#### Mcp

The `Mcp` section of each mapping defines an MCP tool as defined in the [MCP Tools](https://modelcontextprotocol.io/specification/2025-06-18/server/tools) specification:

|Name|Type|Description|Example|
|----|----|-----------|-------|
|name|`string` (required)|Unique identifier for the tool|"get_user"|
|title|`string`|Optional human-readable name of the tool for display purposes|"Retrieve user information by id"|
|inputSchema|JSON Schema|JSON Schema defining expected input parameters|{ "type": "object", "properties": { "userId": { "type": "string", "description": "The user's unique identifier" } }, "required": ["userId"] }|
|outputSchema|JSON Schema|Optional JSON Schema defining the expected response structure|{ "type": "object", "properties": { "id": { "type": "string" }, "name": { "type": "string" } } }|

On each tool call, the provided parameters are validated against the `inputSchema` using [JsonSchema.Net](https://www.nuget.org/packages/JsonSchema.Net).

The `outputSchema` is automatically extracted from OpenAPI 200/201 response definitions when generating tool mappings from Swagger/OpenAPI specifications. It describes the structure of successful responses and enables clients to validate and parse tool results.

Note that the `annotations` property defined in the MCP specification is currently not supported.

#### Rest

The `Rest` section of each mapping describes how Mcpifier calls the target REST API when a tool is invoked:

|Name|Type|Description|Example|
|----|----|-----------|-----|
|method|`string` (required)|HTTP method to use for the REST call|"GET"|
|path|`string` (required)|The template for the URL path to the base address|"/users/\{userId\}"|
|query|`string`|The optional template for the query string appended to the request|"include=profile",<br>"from=\{from\}&to=\{to\}"|
|body|`string`|The optional template for the request JSON body|"{ \"timestamp\": \{timestamp\}, \"metrics\": \{metrics\} }"|

The `path`, `query` and `body` settings all support argument interpolation as defined in the [Argument Interpolation](#argument-interpolation) section below.

### Argument Interpolation

When executing a REST API request, any placeholders in the form `{argName}` in the `path`, `query` and `body` settings are automatically populated with the corresponding argument value from the MCP tool call's `arguments` object.

#### Path Interpolation

Path templates support argument placeholders, for example:

```json
"path": "/users/{userId}/posts/{postId}"
```

Path placeholders are replaced with the string value of the argument _without quotes_, for example:

```
/users/123/posts/abc
```

Path placeholders for absent arguments are replaced with an empty string. This will usually result in an invalid path, so any arguments used in a path template should be marked as `required` in the `inputSchema`.

#### Query String Interpolation

Query string templates support argument placeholders, for example:

```json
"query": "from={startDate}&to={endDate}&limit={maxResults}"
```

Query string placeholders are replaced with the string value of the argument _without quotes_, for example:

```
from=2025-12-01&to=2025-12-31&limit=10
```

Query string placeholders for absent arguments are _removed from the interpolated query along with the parameter definition itself_ if the parameter definition is of the form `"paramName={argName}"`, for example:

```
from=2025-12-01
```

#### Request Body Interpolation

Request body templates support argument placeholders, for example:

```json
"body": "{ \"name\": {userName}, \"email\": {userEmail}, \"tags\": {tags} }"
```

Request body placeholders are replaced with the _JSON serialized representation_ of the argument, for example:

```json
{ "name": "John Doe", "email": "johndoe@example.com", "tags": { "type": "user" } }
```

Request body placeholders for absent arguments are replaced with `null`, for example:

```json
{ "name": "John Doe", "email": "johndoe@example.com", "tags": null }
```

## Dependencies

This package has the following dependencies:

- **.NET 8.0** or later
- **JsonSchema.Net**: JSON Schema validation
- **Microsoft.AspNetCore.Http.Abstractions**: HTTP context abstractions
- **Microsoft.Extensions.Hosting.Abstractions**: Hosting abstractions
- **Microsoft.Extensions.Http**: HTTP client factory
- **Microsoft.OpenApi**: OpenAPI/Swagger specification parsing

## Resources

### Mcpifier

- [Mcpifier GitHub repository](https://github.com/summerdawn-ai/mcpifier)
- [Mcpifier releases and changelog](https://github.com/summerdawn-ai/mcpifier/releases)

### Model Context Protocol

- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/2025-06-18)
- [MCP Authorization documentation](https://modelcontextprotocol.io/docs/tutorials/security/authorization)
- [OAuth 2.0 Protected Resource Metadata](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-resource-metadata)

### .NET

- [.NET Generic Host documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)

## License

This project is licensed under the MIT License.
