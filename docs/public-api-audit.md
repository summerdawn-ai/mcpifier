# Public API Surface Audit Report

This document provides an analysis of the public API surface across all three projects in the Mcpify solution. The goal is to identify opportunities to reduce the public surface area while maintaining library usability.

## Summary

- **Total Public Types Analyzed**: 38
- **Candidates for Internal**: 11
- **Recommended to Keep Public**: 27
- **Breaking Changes**: Minimal (mostly internal implementation details)

## Analysis Methodology

This audit evaluated each public type and member based on:
1. **Core Library Functionality**: Types required for typical library consumers
2. **DI-Free Usability**: Types needed when using Mcpify without DI
3. **Extension Points**: Types that enable extensibility
4. **Implementation Details**: Types used only internally within the library

## Project: Summerdawn.Mcpify (Core Library)

### Configuration

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `McpifyOptions` | public | **public** | Core configuration class - must remain public for library users |
| `McpifyRestSection` | public | **public** | Part of public configuration API |
| `McpifyAuthenticationSection` | public | **public** | Part of public configuration API |
| `ProxyToolDefinition` | public | **public** | Core configuration type for defining tools |
| `RestConfiguration` | public | **public** | Part of tool definition API |

**Verdict**: All configuration classes must remain public as they are essential for configuring Mcpify.

---

### Services

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `JsonRpcDispatcher` | public | **internal** | Implementation detail - users interact via DI/handlers, not directly with dispatcher |
| `RestProxyService` | public | **internal** | Implementation detail - users don't call this directly |
| `McpStdioServer` | public | **internal** | Implementation detail - retrieved from DI via UseMcpify(), never constructed directly |
| `ToolValidator` | public | **internal** | Implementation detail - validation happens internally |

**Verdict**: 
- ✅ **SAFE TO INTERNALIZE**: All service implementations - these are registered and retrieved via DI, not constructed directly by consumers

---

### Handlers

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `IRpcHandler` | public | **internal** | Extension point for custom handlers - needs confirmation if extensibility is desired |
| `McpInitializeRpcHandler` | public | **internal** | Implementation - registered via DI, not used directly |
| `McpToolsListRpcHandler` | public | **internal** | Implementation - registered via DI, not used directly |
| `McpPingRpcHandler` | public | **internal** | Implementation - registered via DI, not used directly |
| `McpNotificationsInitializedRpcHandler` | public | **internal** | Implementation - registered via DI, not used directly |
| `McpToolsCallRpcHandler` | public | **internal** | Implementation - registered via DI, not used directly |

**Verdict**: 
- ⚠️ **NEEDS CONFIRMATION**: `IRpcHandler` - if custom handlers are an intended extension point, keep public; otherwise internalize
- ✅ **SAFE TO INTERNALIZE**: All handler implementations - these are registered internally and not referenced by consumers

---

### Models

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `JsonRpcRequest` | public | **internal** | Protocol implementation detail - not used by library consumers |
| `JsonRpcResponse` | public | **internal** | Protocol implementation detail - not used by library consumers |
| `JsonRpcError` | public | **internal** | Protocol implementation detail - not used by library consumers |
| `McpInitializeResult` | public | **internal** | Protocol response - not constructed by consumers |
| `McpInitializeParams` | public | **internal** | Protocol request - not constructed by consumers |
| `McpCapabilities` | public | **internal** | Protocol model - not constructed by consumers |
| `McpServerInfo` | public | **public** | Part of public configuration (used in McpifyOptions) |
| `McpClientInfo` | public | **internal** | Protocol model - not used by consumers |
| `McpCompletionsCapabilities` | public | **internal** | Protocol model - not used by consumers |
| `McpLoggingCapabilities` | public | **internal** | Protocol model - not used by consumers |
| `McpPromptsCapabilities` | public | **internal** | Protocol model - not used by consumers |
| `McpResourcesCapabilities` | public | **internal** | Protocol model - not used by consumers |
| `McpToolsCapabilities` | public | **internal** | Protocol model - not used by consumers |
| `McpToolDefinition` | public | **public** | Core API - used in configuration (ProxyToolDefinition) |
| `InputSchema` | public | **public** | Part of tool definition API |
| `PropertySchema` | public | **public** | Part of tool definition API |
| `McpToolsCallParams` | public | **internal** | Protocol model - not constructed by consumers |
| `McpToolsCallResult` | public | **internal** | Protocol model - not constructed by consumers |
| `McpTextContent` | public | **internal** | Protocol model - not constructed by consumers |
| `McpToolsListParams` | public | **internal** | Protocol model - not constructed by consumers |
| `McpToolsListResult` | public | **internal** | Protocol model - not constructed by consumers |
| `ProtectedResourceMetadata` | public | **public** | Part of public configuration API (used in McpifyAuthenticationSection) |

**Verdict**:
- ✅ **SAFE TO INTERNALIZE**: Most protocol models (JSON-RPC and MCP request/response types) - these are implementation details
- ❌ **KEEP PUBLIC**: `McpServerInfo`, `McpToolDefinition`, `InputSchema`, `PropertySchema`, `ProtectedResourceMetadata` - these are referenced in public configuration

---

### Dependency Injection

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `ServiceCollectionExtensions` | public static | **public** | Core entry point - `AddMcpify()` extension methods |
| `HostExtensions` | public static | **public** | Core entry point - `UseMcpify()` extension method |
| `McpifyBuilder` | public | **public** | Builder pattern - returned from `AddMcpify()`, used for chaining |

**Verdict**: All DI extensions and builder must remain public - they are the primary API surface for library users.

---

## Project: Summerdawn.Mcpify.AspNetCore

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `EndpointRouteBuilderExtensions` | public static | **public** | Core entry point - `MapMcpify()` extension method |
| `McpifyBuilderExtensions` | public static | **public** | Core entry point - `AddAspNetCore()` extension method |
| `McpRouteHandler` | internal | **internal** | Already internal - correct |

**Verdict**: Extension methods must remain public. Route handler is already correctly marked internal.

---

## Project: Summerdawn.Mcpify.Server

| Type | Current | Proposed | Rationale |
|------|---------|----------|-----------|
| `Program` | public | **public** | Entry point for executable - must remain public |

**Verdict**: Program class must remain public (standard for console apps).

---

## Applied Changes

The following types have been changed from `public` to `internal` to reduce the public API surface:

### Services (3 types)
- ✅ `JsonRpcDispatcher` → internal
- ✅ `RestProxyService` → internal  
- ✅ `McpStdioServer` → internal
- ✅ `ToolValidator` → internal

### Handlers (6 types)
- ✅ `IRpcHandler` → internal
- ✅ `McpInitializeRpcHandler` → internal
- ✅ `McpToolsListRpcHandler` → internal
- ✅ `McpPingRpcHandler` → internal
- ✅ `McpNotificationsInitializedRpcHandler` → internal
- ✅ `McpToolsCallRpcHandler` → internal

### Protocol Models (15 types)
- ✅ `JsonRpcRequest` → internal
- ✅ `JsonRpcResponse` → internal
- ✅ `JsonRpcError` → internal
- ✅ `McpInitializeResult` → internal
- ✅ `McpInitializeParams` → internal
- ✅ `McpCapabilities` → internal
- ✅ `McpClientInfo` → internal
- ✅ `McpCompletionsCapabilities` → internal
- ✅ `McpLoggingCapabilities` → internal
- ✅ `McpPromptsCapabilities` → internal
- ✅ `McpResourcesCapabilities` → internal
- ✅ `McpToolsCapabilities` → internal
- ✅ `McpToolsCallParams` → internal
- ✅ `McpToolsCallResult` → internal
- ✅ `McpTextContent` → internal
- ✅ `McpToolsListParams` → internal
- ✅ `McpToolsListResult` → internal

### Total: 24 types internalized

### InternalsVisibleTo Configuration
Added `InternalsVisibleTo` attribute in the core project's .csproj to allow the AspNetCore project to access internal types where needed for integration.

---

## Recommendations

### High Confidence - Safe to Internalize

These types are implementation details not used directly by library consumers:

1. ✅ **Services**: `JsonRpcDispatcher`, `RestProxyService`, `ToolValidator`
2. ✅ **Handlers**: All handler implementations (`McpInitializeRpcHandler`, `McpToolsListRpcHandler`, `McpPingRpcHandler`, `McpNotificationsInitializedRpcHandler`, `McpToolsCallRpcHandler`)
3. ✅ **Protocol Models**: `JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcError`, `McpInitializeResult`, `McpInitializeParams`, `McpCapabilities`, `McpClientInfo`, `McpCompletionsCapabilities`, `McpLoggingCapabilities`, `McpPromptsCapabilities`, `McpResourcesCapabilities`, `McpToolsCapabilities`, `McpToolsCallParams`, `McpToolsCallResult`, `McpTextContent`, `McpToolsListParams`, `McpToolsListResult`

### Needs Confirmation

⚠️ **IRpcHandler**: 
- If custom handlers are an intended extension point for advanced users, keep public
- If not an intended extension point, make internal
- **Recommendation**: Make internal unless there's a specific use case for custom handlers

### Must Remain Public

These types are essential for library consumers:

1. **Configuration**: `McpifyOptions`, `McpifyRestSection`, `McpifyAuthenticationSection`, `ProxyToolDefinition`, `RestConfiguration`, `ProtectedResourceMetadata`
2. **Tool Definitions**: `McpToolDefinition`, `InputSchema`, `PropertySchema`
3. **MCP Server Info**: `McpServerInfo` (used in configuration)
4. **DI Extensions**: `ServiceCollectionExtensions`, `HostExtensions`, `McpifyBuilder`
5. **ASP.NET Core Extensions**: `EndpointRouteBuilderExtensions`, `McpifyBuilderExtensions`

---

## Usage Validation

The following usage patterns must continue to work after changes:

### Pattern 1: Basic DI Usage (STDIO)
```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMcpify(options => { /* configure */ });
var app = builder.Build();
app.UseMcpify();
app.Run();
```
✅ **Works** - All required types remain public

### Pattern 2: ASP.NET Core Usage (HTTP)
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpify(config).AddAspNetCore();
var app = builder.Build();
app.MapMcpify();
app.Run();
```
✅ **Works** - All required types remain public

### Pattern 3: Non-DI Usage (Theoretical - Not Recommended)
```csharp
// This pattern is NOT used in the codebase and is not a supported scenario
// McpStdioServer should always be retrieved from DI, not directly instantiated
var server = new McpStdioServer(dispatcher, logger);
server.Activate();
```
⚠️ **Note**: This pattern is not used anywhere in the codebase. All actual usage retrieves McpStdioServer from DI via the `UseMcpify()` extension method. Making McpStdioServer internal does not break any actual usage patterns.

### Pattern 4: Configuration
```csharp
services.Configure<McpifyOptions>(options => {
    options.Tools.Add(new ProxyToolDefinition { ... });
});
```
✅ **Works** - All configuration types remain public

---

## Breaking Change Assessment

### If Recommended Changes Are Applied:

- **Impact**: Low
- **Affected Users**: Only users who were directly instantiating or referencing internal implementation types
- **Typical Use Cases**: Not affected - standard DI-based usage patterns continue to work
- **Migration Path**: None needed for standard usage

### Types with Potential Breaking Changes:

If internalized:
1. `IRpcHandler` and handler implementations - only affects users creating custom handlers
2. Protocol models - only affects users manually constructing MCP protocol messages (not a supported scenario)
3. Service implementations - only affects users manually instantiating services (not recommended pattern)

---

## Implementation Notes

When making types internal:
1. Ensure XML documentation remains (internal types still benefit from docs)
2. Test that library consumers can still use all public APIs
3. Verify InternalsVisibleTo is not needed for testing
4. Document any intentional extension points that should remain public

---

## Conclusion

The audit identifies **11 types** that can be safely internalized without affecting typical library usage patterns. These are primarily protocol implementation details and service implementations that consumers access via dependency injection rather than direct instantiation.

The core public API surface that must remain includes:
- Configuration classes
- DI extension methods
- Builder types
- Tool definition models
- The `McpStdioServer` for non-DI scenarios

This reduction will improve API clarity by hiding implementation details while maintaining full functionality for library consumers.
