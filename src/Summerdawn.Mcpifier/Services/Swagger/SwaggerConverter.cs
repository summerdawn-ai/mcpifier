using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

using Json.Schema;

using Microsoft.OpenApi;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Converts OpenAPI/Swagger specifications to Mcpifier tool mappings and related options.
/// </summary>
public class SwaggerConverter(IHttpClientFactory httpClientFactory, ILogger<SwaggerConverter> logger)
{
    /// <summary>
    /// Loads the specified Swagger specification, converts it into mappings, and saves them as JSON to the output file.
    /// </summary>
    /// <param name="swaggerFileNameOrUrl">The file name or URL of the Swagger/OpenAPI specification.</param>
    /// <param name="outputFileName">The output file name, default "mappings.json".</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "SwaggerConverterJsonContext supports all types in MinimalOptionsWrapper")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "SwaggerConverterJsonContext supports all types in MinimalOptionsWrapper")]
    public async Task LoadAndConvertAsync(string swaggerFileNameOrUrl, string? outputFileName = null)
    {
        outputFileName ??= "mappings.json";

        try
        {
            // Parse Swagger into tools.
            logger.LogInformation("Loading Swagger specification from '{Source}'.", swaggerFileNameOrUrl);

            // Read Swagger file (support file or URL).
            string swaggerJson = await LoadSwaggerAsync(swaggerFileNameOrUrl);

            // Parse Swagger into tools.
            var swaggerOptions = await ConvertAsync(swaggerJson);
            var (swaggerTools, swaggerBaseAddress) = (swaggerOptions.Tools, swaggerOptions.Rest?.BaseAddress);

            logger.LogInformation("Created {Count} tool mappings from Swagger specification '{Source}'.", swaggerTools.Count, swaggerFileNameOrUrl);

            // Save tools as mappings.json - use minimal options structure with
            // custom serializer to avoid writing null values or empty properties.
            var minimalOptions = new MinimalOptionsWrapper
            {
                Mcpifier = new()
                {
                    Rest = swaggerBaseAddress is null ? null : new() { BaseAddress = swaggerBaseAddress },
                    Tools = swaggerTools
                }
            };

            string mappingsJson = JsonSerializer.Serialize<MinimalOptionsWrapper>(minimalOptions, SwaggerConverterJsonContext.JsonOptions);

            await File.WriteAllTextAsync(outputFileName, mappingsJson);

            logger.LogInformation("Saved tool mappings to '{FileName}'.", outputFileName);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to create tool mappings from Swagger specification '{Source}'.", swaggerFileNameOrUrl);
            throw new InvalidOperationException($"Failed to create tool mappings from Swagger specification '{swaggerFileNameOrUrl}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts a Swagger/OpenAPI specification file to a list of tool mappings.
    /// </summary>
    /// <param name="swaggerJson">The specification JSON content.</param>
    /// <returns>An instance of <see cref="McpifierOptions"/> containing the resulting tool
    /// mappings and options.</returns>
    public async Task<McpifierOptions> ConvertAsync(string swaggerJson)
    {
        OpenApiDocument document;
        try
        {
            var result = OpenApiDocument.Parse(swaggerJson);

            (var openApiDiagnostic, document) = (result.Diagnostic!, result.Document!);

            // Log any diagnostics from parsing
            if (openApiDiagnostic.Errors.Count > 0)
            {
                foreach (var error in openApiDiagnostic.Errors)
                {
                    logger.LogError("OpenAPI parsing error: {Message} at {Pointer}", error.Message, error.Pointer);
                }
                throw new InvalidOperationException("Failed to parse OpenAPI document. See logs for details.");
            }

            if (openApiDiagnostic.Warnings.Count > 0)
            {
                foreach (var warning in openApiDiagnostic.Warnings)
                {
                    logger.LogWarning("OpenAPI parsing warning: {Message} at {Pointer}", warning.Message, warning.Pointer);
                }
            }
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            logger.LogError(ex, "Failed to read or parse OpenAPI document");
            throw new InvalidOperationException($"Failed to read or parse OpenAPI document: {ex.Message}", ex);
        }

        var tools = new List<McpifierToolMapping>();

        foreach (var pathItem in document.Paths)
        {
            string path = pathItem.Key;
            var operations = pathItem.Value.Operations ?? [];

            foreach (var operation in operations)
            {
                try
                {
                    var tool = await ConvertOperationAsync(path, operation.Key, operation.Value);
                    tools.Add(tool);
                    logger.LogDebug("Converted operation: {Method} {Path} -> {ToolName}", operation.Key, path, tool.Mcp.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to convert operation {Method} {Path}: {Message}",
                        operation.Key, path, ex.Message);
                }
            }
        }

        logger.LogDebug("Successfully converted {Count} operations to tools.", tools.Count);

        string? baseAddress = document.Servers?.FirstOrDefault()?.Url;

        // Return options with only tools and base address set for easy serialization.
        return new McpifierOptions
        {
            Rest = baseAddress is null ? null! : new McpifierRestSection
            {
                BaseAddress = baseAddress
            },
            ProtocolVersion = null!,
            ServerInfo = null!,
            Instructions = null!,
            Tools = tools,
            Authorization = null!,
        };
    }

    /// <summary>
    /// Converts a single OpenAPI operation to a Mcpifier tool mapping.
    /// </summary>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>A tool mapping.</returns>
    private async Task<McpifierToolMapping> ConvertOperationAsync(string path, HttpMethod type, OpenApiOperation operation)
    {
        string toolName = GenerateToolName(operation, path, type);
        var (schemaObject, schemaElement) = await BuildInputSchemaAsync(operation);
        var restConfig = BuildRestConfiguration(path, type, operation);

        return new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = toolName,
                Description = operation.Summary ?? operation.Description ?? $"{type} {path}",
                InputSchemaObject = schemaObject,
                InputSchema = schemaElement
            },
            Rest = restConfig
        };
    }

    /// <summary>
    /// Builds the input schema from operation parameters and request body.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>A tuple containing the JsonSchema object and JsonElement for serialization.</returns>
    [Obsolete]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private async Task<(JsonSchema schemaObject, JsonElement schemaElement)> BuildInputSchemaAsync(OpenApiOperation operation)
    {
        var inputSchema = new InputSchema();

        // Create initial input schema based on request body, if present.
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var requestBody) == true)
        {
            var schema = ResolveSchema(requestBody.Schema);

            // If the request body is an object, use it as the base of the InputSchema;
            // otherwise, add the request body as a single property "requestBody".
            if (schema is { Type: JsonSchemaType.Object })
            {
                // Serialize as not-quite-JSON-Schema v3.0 so we don't get type
                // arrays for nullable types that break our deserialization.
                string json = await schema.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
                inputSchema = JsonSerializer.Deserialize<InputSchema>(json)!;
            }
            else if (schema is not null)
            {
                // Serialize as not-quite-JSON-Schema v3.0 so we don't get type
                // arrays for nullable types that break our deserialization.
                string json = await schema.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
                inputSchema.Properties = new()
                {
                    ["requestBody"] = JsonSerializer.Deserialize<PropertySchema>(json)!
                };
            }
        }

        foreach (var param in operation.Parameters ?? [])
        {
            var schema = ResolveSchema(param.Schema);

            if (schema is null)
            {
                // Skip parameters without schema definition.
                continue;
            }

            // Serialize as not-quite-JSON-Schema v3.0 so we don't get type
            // arrays for nullable types that break our deserialization.
            string json = await schema.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
            var propertySchema = JsonSerializer.Deserialize<PropertySchema>(json)!;

            // Prefer param description over schema description.
            propertySchema.Description = param.Description ?? propertySchema.Description;

            inputSchema.Properties ??= [];
            inputSchema.Properties[param.Name!] = propertySchema;
            if (param.Required)
            {
                inputSchema.Required.Add(param.Name!);
            }
        }

        // Serialize the inputSchema to JSON string
        string schemaJson = JsonSerializer.Serialize(inputSchema);

        // Parse into JsonSchema for validation
        var schemaObject = JsonSchema.FromText(schemaJson);

        // Parse into JsonElement for serialization
        using var document = JsonDocument.Parse(schemaJson);
        var schemaElement = document.RootElement.Clone();

        return (schemaObject, schemaElement);
    }

    /// <summary>
    /// Builds the REST configuration from the operation.
    /// </summary>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>A REST configuration.</returns>
    private RestConfiguration BuildRestConfiguration(string path, HttpMethod type, OpenApiOperation operation)
    {
        var config = new RestConfiguration
        {
            Method = type.ToString().ToUpperInvariant(),
            Path = path
        };

        // Build query string from query parameters
        var queryParams = operation.Parameters?
            .Where(p => p.In == ParameterLocation.Query)
            .Select(p => $"{p.Name}={{{p.Name}}}")
            .ToList();

        if (queryParams?.Count > 0)
        {
            config.Query = string.Join("&", queryParams);
        }

        // Build request body template
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) == true)
        {
            var schema = ResolveSchema(mediaType.Schema);

            if (schema is { Type: JsonSchemaType.Object })
            {
                // Request body is object, so build body template from individual properties.
                if (schema.Properties?.Count > 0)
                {
                    var bodyParts = schema.Properties.Select(prop => $"\"{prop.Key}\": {{{prop.Key}}}");

                    config.Body = "{ " + string.Join(", ", bodyParts) + " }";
                }
                else
                {
                    config.Body = "{}";
                }
            }
            else if (schema is not null)
            {
                // Otherwise, we added the body to the input schema as a
                // single property "requestBody", so reflect that here.
                config.Body = "{requestBody}";
            }
        }

        return config;
    }

    /// <summary>
    /// Generates a snake_case tool name from the operation.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <returns>A snake_case tool name.</returns>
    private static string GenerateToolName(OpenApiOperation operation, string path, HttpMethod type)
    {
        if (!string.IsNullOrWhiteSpace(operation.OperationId))
        {
            return ToSnakeCase(operation.OperationId);
        }

        // Generate from method and path
        string methodPart = type.ToString().ToLowerInvariant();
        string pathPart = path.TrimStart('/').Replace('/', '_').Replace('{', ' ').Replace('}', ' ');
        pathPart = Regex.Replace(pathPart, @"\s+", "_");
        pathPart = Regex.Replace(pathPart, @"[^a-zA-Z0-9_]", "");

        return ToSnakeCase($"{methodPart}_{pathPart}");
    }

    /// <summary>
    /// Converts text to snake_case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The snake_case version of the text.</returns>
    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Insert underscores before uppercase letters (except at the start)
        string result = Regex.Replace(text, @"([a-z0-9])([A-Z])", "$1_$2");

        // Convert to lowercase
        result = result.ToLowerInvariant();

        // Replace any non-alphanumeric characters with underscores
        result = Regex.Replace(result, @"[^a-z0-9_]", "_");

        // Remove duplicate underscores
        result = Regex.Replace(result, @"_+", "_");

        // Remove leading/trailing underscores
        result = result.Trim('_');

        return result;
    }

    /// <summary>
    /// Loads a Swagger specification from a file or URL.
    /// </summary>
    /// <param name="fileNameOrUrl">The file name or URL.</param>
    /// <returns>The file contents.</returns>
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

    /// <summary>
    /// Resolves any references (including nested) in the specified schema.
    /// </summary>
    private static IOpenApiSchema? ResolveSchema(IOpenApiSchema? schema)
    {
        return ResolveSchemaTree(schema, new HashSet<IOpenApiSchema>(SchemaReferenceEqualityComparer.Instance));
    }

    /// <summary>
    /// Recursively resolves references throughout a schema subtree while avoiding cycles.
    /// </summary>
    /// <param name="schema">The schema to resolve.</param>
    /// <param name="visited">Set used to track visited schemas.</param>
    /// <returns>The resolved schema, or null if none.</returns>
    private static IOpenApiSchema? ResolveSchemaTree(IOpenApiSchema? schema, HashSet<IOpenApiSchema> visited)
    {
        schema = UnwrapReference(schema);

        if (schema is null || !visited.Add(schema))
        {
            return schema;
        }

        if (schema is OpenApiSchema concrete)
        {
            concrete.Items = ResolveSchemaTree(concrete.Items, visited);
            concrete.Not = ResolveSchemaTree(concrete.Not, visited);
            concrete.AdditionalProperties = ResolveSchemaTree(concrete.AdditionalProperties, visited);

            if (concrete.Properties is not null && concrete.Properties.Count > 0)
            {
                string[] propertyNames = concrete.Properties.Keys.ToArray();
                foreach (string propertyName in propertyNames)
                {
                    IOpenApiSchema? resolvedProperty = ResolveSchemaTree(concrete.Properties[propertyName], visited);
                    if (resolvedProperty is null)
                    {
                        concrete.Properties.Remove(propertyName);
                    }
                    else
                    {
                        concrete.Properties[propertyName] = resolvedProperty;
                    }
                }
            }

            ResolveSchemaCollection(concrete.AllOf, visited);
            ResolveSchemaCollection(concrete.AnyOf, visited);
            ResolveSchemaCollection(concrete.OneOf, visited);
        }

        visited.Remove(schema);
        return schema;
    }

    /// <summary>
    /// Resolves references for every schema within a collection.
    /// </summary>
    /// <param name="collection">The schema collection to process.</param>
    /// <param name="visited">Set used to track visited schemas.</param>
    private static void ResolveSchemaCollection(IList<IOpenApiSchema>? collection, HashSet<IOpenApiSchema> visited)
    {
        if (collection is null)
        {
            return;
        }

        for (int index = 0; index < collection.Count; index++)
        {
            IOpenApiSchema? resolvedItem = ResolveSchemaTree(collection[index], visited);
            if (resolvedItem is not null)
            {
                collection[index] = resolvedItem;
            }
        }
    }

    /// <summary>
    /// Follows schema references until a concrete schema instance is reached.
    /// </summary>
    /// <param name="schema">The schema to unwrap.</param>
    /// <returns>The target schema after unwrapping references, or null.</returns>
    private static IOpenApiSchema? UnwrapReference(IOpenApiSchema? schema)
    {
        while (schema is OpenApiSchemaReference reference)
        {
            schema = reference.Target;
        }

        return schema;
    }

    private sealed class SchemaReferenceEqualityComparer : IEqualityComparer<IOpenApiSchema>
    {
        public static SchemaReferenceEqualityComparer Instance { get; } = new SchemaReferenceEqualityComparer();

        public bool Equals(IOpenApiSchema? x, IOpenApiSchema? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(IOpenApiSchema obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
