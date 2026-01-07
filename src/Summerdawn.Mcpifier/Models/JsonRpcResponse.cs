using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents a JSON-RPC 2.0 response.
/// </summary>
public sealed class JsonRpcResponse
{
    /// <summary>
    /// Gets an empty result dictionary used for successful responses without a result.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, object?> EmptyResult = new Dictionary<string, object?>().AsReadOnly();

    /// <summary>
    /// Gets a JsonElement representing a null identifier.
    /// </summary>
    private static readonly JsonElement NullId = JsonDocument.Parse("null").RootElement.Clone();

    // JSON-RPC 2.0 error codes
    private const int InvalidRequestCode = -32600;
    private const int MethodNotFoundCode = -32601;
    private const int InvalidParamsCode = -32602;
    private const int InternalErrorCode = -32603;
    private const int ParseErrorCode = -32700;

    /// <summary>
    /// Gets or sets the JSON-RPC version. Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string Version { get; init; } = "2.0";

    /// <summary>
    /// Gets or sets the request identifier this response corresponds to.
    /// </summary>
    public required JsonElement Id { get; init; }

    /// <summary>
    /// Gets or sets the result of a successful request.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    /// <summary>
    /// Gets or sets the error of a failed request.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; init; }

    /// <summary>
    /// Determines whether this response is empty (has neither result nor error).
    /// </summary>
    /// <returns><c>true</c> if the response is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty() => Result is null && Error is null;

    /// <summary>
    /// Determines whether this response represents an error.
    /// </summary>
    /// <returns><c>true</c> if the response is an error; otherwise, <c>false</c>.</returns>
    public bool IsError() => Error is not null || Result is McpToolsCallResult { IsError: true };

    /// <summary>
    /// Gets an empty response for notifications.
    /// </summary>
    public static JsonRpcResponse Empty { get; } = new()
    {
        Id = NullId
    };

    /// <summary>
    /// Creates a successful JSON-RPC response.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="result">The result value.</param>
    /// <returns>A successful JSON-RPC response.</returns>
    public static JsonRpcResponse Success(JsonElement id, object? result = null) => new()
    {
        Id = id,
        Result = result ?? EmptyResult
    };

    /// <summary>
    /// Creates a parse error response.
    /// </summary>
    /// <returns>A parse error response.</returns>
    public static JsonRpcResponse ParseError() => ErrorResponse(NullId, ParseErrorCode, "Parse Error");

    /// <summary>
    /// Creates an invalid request error response.
    /// </summary>
    /// <returns>An invalid request error response.</returns>
    public static JsonRpcResponse InvalidRequest() => InvalidRequest(NullId);

    /// <summary>
    /// Creates an invalid request error response.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>An invalid request error response.</returns>
    public static JsonRpcResponse InvalidRequest(JsonElement id) => ErrorResponse(id, InvalidRequestCode, "Invalid Request");

    /// <summary>
    /// Creates an invalid params error response.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="message">The error message.</param>
    /// <returns>An invalid params error response.</returns>
    public static JsonRpcResponse InvalidParams(JsonElement id, string message) => ErrorResponse(id, InvalidParamsCode, message);

    /// <summary>
    /// Creates an internal error response.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="message">The error message.</param>
    /// <returns>An internal error response.</returns>
    public static JsonRpcResponse InternalError(JsonElement id, string message) => ErrorResponse(id, InternalErrorCode, message);

    /// <summary>
    /// Creates a method not found error response.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="methodName">The method name that was not found.</param>
    /// <returns>A method not found error response.</returns>
    public static JsonRpcResponse MethodNotFound(JsonElement id, string methodName) => ErrorResponse(id, MethodNotFoundCode, $"Method '{methodName}' not found");

    /// <summary>
    /// Creates an error response with the specified code, message, and data.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="data">Optional additional error data.</param>
    /// <returns>An error response.</returns>
    public static JsonRpcResponse ErrorResponse(JsonElement id, int code, string message, object? data = null) => new()
    {
        // Substitute undefined/default id values with null so they can be serialized.
        Id = id.ValueKind == JsonValueKind.Undefined ? NullId : id,
        Result = null,
        Error = new JsonRpcError
        {
            Code = code,
            Message = message,
            Data = data
        }
    };
}

/// <summary>
/// Represents a JSON-RPC error.
/// </summary>
public sealed class JsonRpcError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; init; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}
