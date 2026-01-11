using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// JSON serializer context for AOT-compatible JSON serialization of <see cref="MinimalOptionsWrapper"/>.
/// </summary>
[JsonSerializable(typeof(MinimalOptionsWrapper))]
[JsonSerializable(typeof(JsonElement))] // PropertySchema.Enum
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class SwaggerConverterJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Defines JSON serialization options for AOT-compatible JSON serialization of <see cref="MinimalOptionsWrapper"/>.
    /// </summary>
    /// <remarks>
    /// Use this instead of <see cref="Default"/> to serialize with <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>,
    /// so that &quot; and &amp; in body and query templates are not escaped.
    /// </remarks>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,

        // Needed to enable AOT-compatible JSON serialization
        TypeInfoResolver = new SwaggerConverterJsonContext(),

        // Don't escape special characters such as " and &
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}