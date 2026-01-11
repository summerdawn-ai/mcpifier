using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Post-configures <see cref="McpifierOptions"/> by loading and merging
/// tools from JSON mappings files registered during application setup.
/// </summary>
internal class MappingsConfigurationLoader(IEnumerable<MappingsConfigurationSource> sources, ILogger<MappingsConfigurationLoader> logger) : IPostConfigureOptions<McpifierOptions>
{
    private int hasRun = 0;

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public void PostConfigure(string? name, McpifierOptions options)
    {
        // Only run once (IPostConfigureOptions can be called multiple times).
        if (Interlocked.CompareExchange(ref hasRun, 1, 0) != 0) { return; }

        foreach (var source in sources)
        {
            try
            {
                logger.LogInformation("Loading mappings from '{FileName}'.", source.FileName);

                // Read mappings file
                string mappingsJson = File.ReadAllText(source.FileName);

                // Deserialize into a minimal wrapper that contains Tools
                var wrapper = JsonSerializer.Deserialize<MinimalOptionsWrapper>(mappingsJson, JsonRpcAndMcpJsonContext.JsonOptions);

                if (wrapper?.Mcpifier?.Tools is null || wrapper.Mcpifier.Tools.Count == 0)
                {
                    logger.LogWarning("No tools found in mappings file '{FileName}'.", source.FileName);
                    continue;
                }

                var mappingsTools = wrapper.Mcpifier.Tools;

                // Merge tools into options, preferring mappings.
                options.Tools = mappingsTools.UnionBy(options.Tools, t => t.Mcp.Name).ToList();

                logger.LogInformation("Loaded {Count} tool mappings from '{FileName}'.", mappingsTools.Count, source.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tool mappings from '{FileName}'.", source.FileName);
                throw new InvalidOperationException($"Failed to load tool mappings from '{source.FileName}': {ex.Message}", ex);
            }
        }
    }

    // Minimal wrapper for deserializing mappings files
    private class MinimalOptionsWrapper
    {
        public MinimalMcpifierOptions? Mcpifier { get; set; }
    }

    private class MinimalMcpifierOptions
    {
        public List<McpifierToolMapping>? Tools { get; set; }
    }
}
