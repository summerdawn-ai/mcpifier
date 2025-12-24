using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// JSON serializer context for AOT-compatible JSON serialization of <see cref="MinimalOptionsWrapper"/>.
/// </summary>
[JsonSerializable(typeof(MinimalOptionsWrapper))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class SwaggerConverterJsonContext : JsonSerializerContext;