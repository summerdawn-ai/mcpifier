using System.Text.Json;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Post-configures <see cref="McpifierOptions"/> by loading and merging
/// tools from JSON mappings files registered during application setup.
/// </summary>
internal class MappingsConfigurationLoader(IEnumerable<MappingsConfigurationSource> sources, ILogger<MappingsConfigurationLoader> logger) : IPostConfigureOptions<McpifierOptions>
{
    private volatile int hasRun = 0;

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

                // Deserialize into tools and REST section.
                var mappingsOptions = JsonSerializer.Deserialize<MinimalOptionsWrapper>(mappingsJson, MinimalOptionsJsonContext.Default.MinimalOptionsWrapper)?.Mcpifier;
                var (mappingsTools, mappingsBaseAddress) = (mappingsOptions?.Tools ?? [], mappingsOptions?.Rest?.BaseAddress);

                // Merge tools into options, preferring mappings.
                options.Tools = mappingsTools.UnionBy(options.Tools, t => t.Mcp.Name).ToList();

                logger.LogInformation("Loaded {Count} tool mappings from '{FileName}'.", mappingsTools.Count, source.FileName);

                // Override base address if empty and mappings server address available.
                if (string.IsNullOrEmpty(options.Rest.BaseAddress) && !string.IsNullOrEmpty(mappingsBaseAddress))
                {
                    logger.LogInformation("Overriding base address with '{BaseAddress}' from mapping.", mappingsBaseAddress);
                    options.Rest.BaseAddress = mappingsBaseAddress;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load tool mappings from '{FileName}'.", source.FileName);
                throw new InvalidOperationException($"Failed to load tool mappings from '{source.FileName}': {ex.Message}", ex);
            }
        }
    }
}
