using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// JSON serializer context for AOT-compatible JSON serialization of JSON-RPC and MCP types.
/// </summary>
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(ReadOnlyDictionary<string, object?>))] // JsonRpcResponse.EmptyResult or JsonRpcError.Data
[JsonSerializable(typeof(McpInitializeParams))]
[JsonSerializable(typeof(McpInitializeResult))]
[JsonSerializable(typeof(McpToolDefinition))]
[JsonSerializable(typeof(McpToolsCallParams))]
[JsonSerializable(typeof(McpToolsCallResult))]
[JsonSerializable(typeof(McpTextContent))]
[JsonSerializable(typeof(McpToolsListParams))]
[JsonSerializable(typeof(McpToolsListResult))]
[JsonSerializable(typeof(ProtectedResourceMetadata))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
internal partial class JsonRpcAndMcpJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Defines JSON serialization options for JSON-RPC and MCP types.
    /// </summary>
    /// <remarks>
    /// Use this when the concrete type to be (de-)serialized is not known at compile time (i.e. is generic).
    /// </remarks>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Needed to enable AOT-compatible JSON serialization
        TypeInfoResolver = new JsonRpcAndMcpJsonContext()
    };

}
