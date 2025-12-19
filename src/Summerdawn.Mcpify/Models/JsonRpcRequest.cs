using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpify.Models;

/// <summary>
/// Represents a JSON-RPC 2.0 request.
/// </summary>
public sealed class JsonRpcRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC version. Must be "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the method name to invoke.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public JsonElement Id { get; init; }

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public JsonElement Params { get; init; }

    /// <summary>
    /// Determines whether the JSON-RPC version is valid (2.0).
    /// </summary>
    /// <returns><c>true</c> if the version is valid; otherwise, <c>false</c>.</returns>
    public bool IsValidVersion() => string.Equals(Version, "2.0", StringComparison.Ordinal);

    /// <summary>
    /// Deserializes the parameters to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized parameters, or default if parameters are null or undefined.</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JsonRpcAndMcpJsonContext supports all possible types of T")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "JsonRpcAndMcpJsonContext supports all possible types of T")]
    public T? DeserializeParams<T>()
    {
        if (Params.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return default;
        }

        try
        {
            return Params.Deserialize<T>(JsonRpcAndMcpJsonContext.JsonOptions);

        }
        catch (JsonException e)
        {
            throw new JsonException($"Failed to deserialize params: {e.Message}", e);
        }
    }

    /// <summary>
    /// Deserializes the parameters to the specified type, throwing if parameters are missing.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized parameters.</returns>
    /// <exception cref="JsonException">Thrown when parameters are missing or deserialization fails.</exception>
    public T DeserializeRequiredParams<T>() =>
        DeserializeParams<T>() ?? throw new JsonException("Params are required.");
}
