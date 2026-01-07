using System.Text.Json;

using Summerdawn.Mcpifier.Models;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Provides validation for MCP tool arguments.
/// </summary>
internal static class ToolValidator
{
    /// <summary>
    /// Validates that the provided arguments match the tool's input schema.
    /// </summary>
    /// <param name="mcpTool">The MCP tool definition containing the input schema.</param>
    /// <param name="arguments">The arguments to validate.</param>
    /// <returns>A tuple indicating whether validation succeeded and an optional error message.</returns>
    public static (bool isValid, string? errorMessage) ValidateArguments(McpToolDefinition mcpTool, Dictionary<string, JsonElement> arguments)
    {
        var schema = mcpTool.InputSchema;

        if (schema.Required is { Count: > 0 })
        {
            foreach (string requiredField in schema.Required)
            {
                if (!arguments.TryGetValue(requiredField, out var argument))
                {
                    return (false, $"Required field '{requiredField}' is missing");
                }

                if (argument.ValueKind == JsonValueKind.Null)
                {
                    return (false, $"Required field '{requiredField}' cannot be null");
                }
            }
        }

        // Simple type checking if properties are defined
        if (schema.Properties != null)
        {
            foreach (var arg in arguments)
            {
                if (schema.Properties.TryGetValue(arg.Key, out var propertySchema))
                {
                    if (!ValidateType(arg.Value, propertySchema.Type))
                    {
                        return (false, $"Field '{arg.Key}' has invalid type. Expected: {propertySchema.Type}");
                    }
                }
            }
        }

        return (true, null);
    }

    private static bool ValidateType(JsonElement value, string expectedType)
    {
        // Allow null or undefined unless value is required.
        if (value is { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
        {
            return true;
        }

        return expectedType.ToLower() switch
        {
            "string" => value is { ValueKind: JsonValueKind.String },
            "number" => value is { ValueKind: JsonValueKind.Number },
            "integer" => value is { ValueKind: JsonValueKind.Number },
            "boolean" => value is { ValueKind: JsonValueKind.True or JsonValueKind.False },
            "object" => value is { ValueKind: JsonValueKind.Object },
            "array" => value is { ValueKind: JsonValueKind.Array },
            _ => true // Unknown types pass validation
        };
    }
}
