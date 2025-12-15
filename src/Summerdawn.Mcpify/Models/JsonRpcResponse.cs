using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

public sealed class JsonRpcResponse
{
    public static readonly IReadOnlyDictionary<string, object?> EmptyResult = new Dictionary<string, object?>();

    // JSON-RPC 2.0 error codes
    private const int InvalidRequestCode = -32600;
    private const int MethodNotFoundCode = -32601;
    private const int InvalidParamsCode = -32602;
    private const int InternalErrorCode = -32603;

    [JsonPropertyName("jsonrpc")]
    public string Version { get; init; } = "2.0";

    public required JsonElement Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; init; }

    public bool IsEmpty() => Result is null && Error is null;

    public bool IsError() => Error is not null || Result is McpToolsCallResult { IsError: true };

    // Response for notifications - mapped to HTTP 204 No Content.
    public static JsonRpcResponse Empty { get; } = new()
    {
        Id = default
    };

    public static JsonRpcResponse Success(JsonElement id, object? result = null) => new()
    {
        Id = id,
        Result = result ?? EmptyResult
    };

    public static JsonRpcResponse InvalidRequest(JsonElement id) => ErrorResponse(id, InvalidRequestCode, "Invalid Request");

    public static JsonRpcResponse MethodNotFound(JsonElement id, string methodName) => ErrorResponse(id, MethodNotFoundCode, $"Method '{methodName}' not found");

    public static JsonRpcResponse InvalidParams(JsonElement id, string message) => ErrorResponse(id, InvalidParamsCode, message);

    public static JsonRpcResponse InternalError(JsonElement id, string message) => ErrorResponse(id, InternalErrorCode, message);

    public static JsonRpcResponse ErrorResponse(JsonElement id, int code, string message, object? data = null) => new()
    {
        Id = id,
        Result = null,
        Error = new JsonRpcError
        {
            Code = code,
            Message = message,
            Data = data
        }
    };
}

public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}
