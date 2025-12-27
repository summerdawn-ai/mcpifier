using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Post-configures <see cref="McpifierOptions"/> by loading and merging
/// tools from Swagger sources registered during application setup.
/// </summary>
internal class SwaggerConfigurationLoader(IHttpClientFactory httpClientFactory, IEnumerable<SwaggerConfigurationSource> sources, SwaggerConverter converter, ILogger<SwaggerConfigurationLoader> logger) : IPostConfigureOptions<McpifierOptions>
{
    private int hasRun = 0;

    public void PostConfigure(string? name, McpifierOptions options)
    {
        // Only run once (IPostConfigureOptions can be called multiple times).
        if (Interlocked.CompareExchange(ref hasRun, 1, 0) != 0) return;

        foreach (var source in sources)
        {
            try
            {
                logger.LogInformation("Loading Swagger specification from '{Source}'.", source.FileNameOrUrl);

                // Read Swagger file (support file or URL).
                string swaggerJson = LoadSwaggerAsync(source.FileNameOrUrl).GetAwaiter().GetResult();

                // Parse Swagger into tools.
                var swaggerOptions = converter.ConvertAsync(swaggerJson).GetAwaiter().GetResult();
                var (swaggerTools, swaggerBaseAddress) = (swaggerOptions.Tools, swaggerOptions.Rest?.BaseAddress);

                // Try and infer base address from URL if not defined in Swagger file.
                if (swaggerBaseAddress is null)
                {
                    swaggerBaseAddress = TryGetBaseAddress(source.FileNameOrUrl);
                }
                
                // Apply filter if provided.
                if (source.MappingFilter is not null)
                {
                    swaggerTools = swaggerTools.Where(source.MappingFilter).ToList();
                }

                // Apply action if provided.
                if (source.MappingAction is not null)
                {
                    foreach (var tool in swaggerTools)
                    {
                        source.MappingAction(tool);
                    }
                }

                // Merge tools into options, preferring Swagger.
                options.Tools = swaggerTools.UnionBy(options.Tools, t => t.Mcp.Name).ToList();

                // Override base address if empty and Swagger server address available.
                if (string.IsNullOrEmpty(options.Rest.BaseAddress) && !string.IsNullOrEmpty(swaggerBaseAddress))
                {
                    logger.LogInformation("Overriding base address with '{BaseAddress}' from Swagger.", swaggerBaseAddress);
                    options.Rest.BaseAddress = swaggerBaseAddress;
                }

                logger.LogInformation("Created {Count} tool mappings from Swagger specification '{Source}'.", swaggerTools.Count, source.FileNameOrUrl);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to create tool mappings from Swagger specification '{Source}'.", source.FileNameOrUrl);
                throw new InvalidOperationException($"Failed to create tool mappings from Swagger specification '{source.FileNameOrUrl}': {ex.Message}", ex);
            }
        }
    }

    private static string? TryGetBaseAddress(string fileNameOrUrl)
    {
        if (Uri.TryCreate(fileNameOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        return null;
    }

    private async Task<string> LoadSwaggerAsync(string fileNameOrUrl)
    {
        if (Uri.TryCreate(fileNameOrUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            // Download from URL.
            var httpClient = httpClientFactory.CreateClient();
            return await httpClient.GetStringAsync(uri);
        }
        else
        {
            // Read from file.
            return await File.ReadAllTextAsync(fileNameOrUrl);
        }
    }
}