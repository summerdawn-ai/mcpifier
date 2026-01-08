using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Json.Schema;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Post-configures <see cref="McpifierOptions"/> by loading and merging
/// tools from JSON mappings files registered during application setup.
/// </summary>
internal class MappingsConfigurationLoader(IEnumerable<MappingsConfigurationSource> sources, ILogger<MappingsConfigurationLoader> logger) : IPostConfigureOptions<McpifierOptions>
{
    private int hasRun = 0;

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public void PostConfigure(string? name, McpifierOptions options)
    {
        // Only run once (IPostConfigureOptions can be called multiple times).
        if (Interlocked.CompareExchange(ref hasRun, 1, 0) != 0) { return; }

        foreach (var source in sources)
        {
            try
            {
                logger.LogInformation("Loading JSON mappings from '{FileName}'.", source.FileName);

                // Read JSON file synchronously (PostConfigure cannot be async)
                string json = File.ReadAllText(source.FileName);

                // Deserialize to a wrapper class containing a Tools list
                var wrapper = JsonSerializer.Deserialize<MappingsWrapper>(json);

                if (wrapper?.Mcpifier?.Tools == null || wrapper.Mcpifier.Tools.Count == 0)
                {
                    logger.LogWarning("No tools found in JSON mappings file '{FileName}'.", source.FileName);
                    continue;
                }

                var mappingsTools = wrapper.Mcpifier.Tools;

                // Parse InputSchema JsonElement into InputSchemaObject for each tool
                foreach (var tool in mappingsTools)
                {
                    if (tool.Mcp.InputSchema.ValueKind != JsonValueKind.Undefined && tool.Mcp.InputSchema.ValueKind != JsonValueKind.Null)
                    {
                        string schemaJson = tool.Mcp.InputSchema.GetRawText();
                        tool.Mcp.InputSchemaObject = JsonSchema.FromText(schemaJson);
                    }
                }

                // Merge tools into options, preferring mappings.
                options.Tools = mappingsTools.UnionBy(options.Tools, t => t.Mcp.Name).ToList();

                logger.LogInformation("Loaded {Count} tool mappings from JSON mappings file '{FileName}'.", mappingsTools.Count, source.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tool mappings from JSON mappings file '{FileName}'.", source.FileName);
                throw new InvalidOperationException($"Failed to load tool mappings from JSON mappings file '{source.FileName}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Wrapper class for deserializing JSON mappings files.
    /// </summary>
    private class MappingsWrapper
    {
        public McpifierSection? Mcpifier { get; set; }
    }

    /// <summary>
    /// Mcpifier section in JSON mappings file.
    /// </summary>
    private class McpifierSection
    {
        public List<McpifierToolMapping> Tools { get; set; } = [];
    }
}
