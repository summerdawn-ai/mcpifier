using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Json.Schema;

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
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.SerializeToElement<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.SerializeToElement<TValue>(TValue, JsonSerializerOptions)")]
    public static (bool isValid, string? errorMessage) ValidateArguments(McpToolDefinition mcpTool, Dictionary<string, JsonElement> arguments)
    {
        // Convert arguments dictionary to JsonElement
        var argumentsJson = JsonSerializer.SerializeToElement(arguments);

        // Evaluate schema using Json.Schema.Net
        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        };

        var results = mcpTool.InputSchemaObject.Evaluate(argumentsJson, options);

        if (results.IsValid)
        {
            return (true, null);
        }

        // Build error message from validation results
        var errors = new List<string>();
        CollectErrors(results, errors);

        string errorMessage = errors.Count > 0
            ? string.Join("; ", errors)
            : "Validation failed";

        return (false, errorMessage);
    }

    private static void CollectErrors(EvaluationResults results, List<string> errors)
    {
        if (results.HasErrors && results.Errors != null)
        {
            foreach (var (key, value) in results.Errors)
            {
                errors.Add($"{key}: {value}");
            }
        }

        if (results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                CollectErrors(detail, errors);
            }
        }
    }
}
