using System.Text.RegularExpressions;

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

using Summerdawn.Mcpifier.Configuration;
using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Converts OpenAPI/Swagger specifications to Mcpifier tool mappings.
/// </summary>
public class SwaggerToMappingConverter(ILogger<SwaggerToMappingConverter> logger)
{
    /// <summary>
    /// Converts a Swagger/OpenAPI specification file to a list of proxy tool definitions.
    /// </summary>
    /// <param name="swaggerFilePath">Path to the swagger.json or openapi.json file.</param>
    /// <returns>List of proxy tool definitions.</returns>
    public async Task<List<McpifierToolMapping>> ConvertAsync(string swaggerFilePath)
    {
        if (!File.Exists(swaggerFilePath))
        {
            throw new FileNotFoundException($"Swagger file not found: {swaggerFilePath}");
        }

        OpenApiDocument document;
        try
        {
            using var stream = File.OpenRead(swaggerFilePath);
            var reader = new OpenApiStreamReader();
            var readResult = await reader.ReadAsync(stream);

            document = readResult.OpenApiDocument;

            // Log any diagnostics from parsing
            if (readResult.OpenApiDiagnostic.Errors.Count > 0)
            {
                foreach (var error in readResult.OpenApiDiagnostic.Errors)
                {
                    logger.LogError("OpenAPI parsing error: {Message} at {Pointer}", error.Message, error.Pointer);
                }
                throw new InvalidOperationException("Failed to parse OpenAPI document. See logs for details.");
            }

            if (readResult.OpenApiDiagnostic.Warnings.Count > 0)
            {
                foreach (var warning in readResult.OpenApiDiagnostic.Warnings)
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
            var operations = pathItem.Value.Operations;

            foreach (var operation in operations)
            {
                try
                {
                    var tool = ConvertOperation(path, operation.Key, operation.Value);
                    tools.Add(tool);
                    logger.LogInformation("Converted operation: {Method} {Path} -> {ToolName}",
                        operation.Key, path, tool.Mcp.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to convert operation {Method} {Path}: {Message}",
                        operation.Key, path, ex.Message);
                }
            }
        }

        logger.LogInformation("Successfully converted {Count} operations to tools", tools.Count);
        return tools;
    }

    /// <summary>
    /// Converts a single OpenAPI operation to a proxy tool definition.
    /// </summary>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>A proxy tool definition.</returns>
    private McpifierToolMapping ConvertOperation(string path, OperationType type, OpenApiOperation operation)
    {
        string toolName = GenerateToolName(operation, path, type);
        var inputSchema = BuildInputSchema(operation);
        var restConfig = BuildRestConfiguration(path, type, operation);

        return new McpifierToolMapping
        {
            Mcp = new McpToolDefinition
            {
                Name = toolName,
                Description = operation.Summary ?? operation.Description ?? $"{type} {path}",
                InputSchema = inputSchema
            },
            Rest = restConfig
        };
    }

    /// <summary>
    /// Generates a snake_case tool name from the operation.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <returns>A snake_case tool name.</returns>
    private string GenerateToolName(OpenApiOperation operation, string path, OperationType type)
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
    /// Builds the input schema from operation parameters and request body.
    /// </summary>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>An input schema.</returns>
    private InputSchema BuildInputSchema(OpenApiOperation operation)
    {
        var properties = new Dictionary<string, PropertySchema>();
        var required = new List<string>();

        // Add parameters (path, query, header)
        if (operation.Parameters != null)
        {
            foreach (var param in operation.Parameters)
            {
                var propertySchema = ConvertSchemaToPropertySchema(param.Schema);
                propertySchema.Description = param.Description ?? propertySchema.Description;

                properties[param.Name] = propertySchema;

                if (param.Required)
                {
                    required.Add(param.Name);
                }
            }
        }

        // Add request body properties if present
        if (operation.RequestBody != null)
        {
            if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            {
                if (mediaType.Schema?.Type == "object" && mediaType.Schema.Properties != null)
                {
                    // Flatten object properties into the input schema
                    foreach (var prop in mediaType.Schema.Properties)
                    {
                        properties[prop.Key] = ConvertSchemaToPropertySchema(prop.Value);

                        // Check if required
                        if (mediaType.Schema.Required?.Contains(prop.Key) == true)
                        {
                            required.Add(prop.Key);
                        }
                    }
                }
                else if (mediaType.Schema != null)
                {
                    // If the body schema is not an object, we'll need to handle it differently
                    // For simplicity, we'll skip non-object body schemas in this prototype
                    logger.LogWarning("Skipping non-object request body schema for operation");
                }
            }
        }

        return new InputSchema
        {
            Type = "object",
            Properties = properties.Count > 0 ? properties : null,
            Required = required
        };
    }

    /// <summary>
    /// Converts an OpenAPI schema to a PropertySchema.
    /// </summary>
    /// <param name="schema">The OpenAPI schema.</param>
    /// <returns>A PropertySchema.</returns>
    private PropertySchema ConvertSchemaToPropertySchema(OpenApiSchema schema)
    {
        if (schema == null)
        {
            return new PropertySchema { Type = "string" };
        }

        var propertySchema = new PropertySchema
        {
            Type = schema.Type ?? "string",
            Description = schema.Description,
            Format = schema.Format
        };

        // Handle enum values
        if (schema.Enum != null && schema.Enum.Count > 0)
        {
            propertySchema.Enum = schema.Enum
                .Select(e =>
                {
                    // OpenAPI enum values are IOpenApiAny objects
                    // They can be OpenApiString, OpenApiInteger, etc.
                    // We need to extract the actual primitive value
                    if (e is Microsoft.OpenApi.Any.OpenApiString openApiString)
                    {
                        return (object)openApiString.Value;
                    }
                    else if (e is Microsoft.OpenApi.Any.OpenApiInteger openApiInt)
                    {
                        return (object)openApiInt.Value;
                    }
                    else if (e is Microsoft.OpenApi.Any.OpenApiLong openApiLong)
                    {
                        return (object)openApiLong.Value;
                    }
                    else if (e is Microsoft.OpenApi.Any.OpenApiDouble openApiDouble)
                    {
                        return (object)openApiDouble.Value;
                    }
                    else if (e is Microsoft.OpenApi.Any.OpenApiBoolean openApiBool)
                    {
                        return (object)openApiBool.Value;
                    }
                    else
                    {
                        // Fallback to ToString
                        return (object)(e?.ToString() ?? string.Empty);
                    }
                })
                .ToList();
        }

        // Handle array items
        if (schema.Type == "array" && schema.Items != null)
        {
            propertySchema.Items = ConvertSchemaToPropertySchema(schema.Items);
        }

        // Handle nested object properties
        if (schema.Type == "object" && schema.Properties != null && schema.Properties.Count > 0)
        {
            propertySchema.Properties = schema.Properties.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertSchemaToPropertySchema(kvp.Value)
            );
        }

        return propertySchema;
    }

    /// <summary>
    /// Builds the REST configuration from the operation.
    /// </summary>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP operation type.</param>
    /// <param name="operation">The OpenAPI operation.</param>
    /// <returns>A REST configuration.</returns>
    private RestConfiguration BuildRestConfiguration(string path, OperationType type, OpenApiOperation operation)
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

        if (queryParams != null && queryParams.Count > 0)
        {
            config.Query = string.Join("&", queryParams);
        }

        // Build request body template
        if (operation.RequestBody != null)
        {
            if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            {
                if (mediaType.Schema?.Type == "object" && mediaType.Schema.Properties != null)
                {
                    var bodyParts = new List<string>();
                    foreach (var prop in mediaType.Schema.Properties)
                    {
                        bodyParts.Add($"\"{prop.Key}\": {{{prop.Key}}}");
                    }

                    if (bodyParts.Count > 0)
                    {
                        config.Body = "{ " + string.Join(", ", bodyParts) + " }";
                    }
                }
            }
        }

        return config;
    }

    /// <summary>
    /// Converts text to snake_case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The snake_case version of the text.</returns>
    private string ToSnakeCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Insert underscores before uppercase letters (except at the start)
        var result = Regex.Replace(text, @"([a-z0-9])([A-Z])", "$1_$2");

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
}
