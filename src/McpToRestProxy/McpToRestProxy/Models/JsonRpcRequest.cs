using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.McpToRestProxy.Models;

public sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public required string Version { get; init; }

    public required string Method { get; init; }

    public JsonElement Id { get; init; }

    public JsonElement Params { get; init; }

    public bool IsValidVersion() => string.Equals(Version, "2.0", StringComparison.Ordinal);

    public T? DeserializeParams<T>(JsonSerializerOptions? serializerOptions = default)
    {
        if (Params.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return default;
        }

        try
        {
            return Params.Deserialize<T>(serializerOptions);

        }
        catch (JsonException e)
        {
            throw new JsonException($"Failed to deserialize params: {e.Message}", e);
        }
    }

    public T DeserializeRequiredParams<T>(JsonSerializerOptions? serializerOptions = default) =>
        DeserializeParams<T>(serializerOptions) ?? throw new JsonException("Params are required.");
}
