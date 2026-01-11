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
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.SerializeToElement<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.SerializeToElement<TValue>(TValue, JsonSerializerOptions)")]
    public static (bool isValid, string? errorMessage) ValidateArguments(McpToolDefinition mcpTool, Dictionary<string, JsonElement> arguments)
    {
        var argumentsJson = JsonSerializer.SerializeToElement(arguments);
        var schema = mcpTool.GetDeserializedInputSchema();

        var validationResults = schema.Evaluate(argumentsJson, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        if (validationResults.IsValid)
        {
            return (true, null);
        }

        string errorMessage = BuildErrorMessage(validationResults);
        return (false, errorMessage);
    }

    private static string BuildErrorMessage(EvaluationResults results)
    {
        var errors = results.Details
            .Where(d => !d.IsValid)
            .Select(d =>
            {
                string location = d.InstanceLocation.ToString();
                string message = d.Errors?.FirstOrDefault().Value ?? "Validation failed";
                return string.IsNullOrEmpty(location) ? message : $"{location}: {message}";
            })
            .ToList();

        if (errors.Count == 0)
        {
            return "Validation failed";
        }

        return string.Join("; ", errors);
    }
}
