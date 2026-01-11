using System.Text.RegularExpressions;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Generates tool names from OpenAPI operations.
/// </summary>
internal static class ToolNameGenerator
{
    /// <summary>
    /// Generates a tool name from an operation ID by converting it to snake_case.
    /// </summary>
    /// <param name="operationId">The operation ID.</param>
    /// <returns>A snake_case tool name.</returns>
    public static string GenerateFromOperationId(string operationId)
    {
        return ToSnakeCase(operationId);
    }

    /// <summary>
    /// Generates a tool name from a summary by extracting meaningful keywords.
    /// </summary>
    /// <param name="summary">The operation summary.</param>
    /// <returns>A snake_case tool name, or null if extraction fails.</returns>
    public static string? GenerateFromSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 50)
        {
            return null;
        }

        // Remove common articles and prepositions
        string cleaned = summary;
        cleaned = Regex.Replace(cleaned, @"\b(a|an|the|by|to|from|in|on|at|for|of)\b", "", RegexOptions.IgnoreCase);

        // Extract remaining words
        var words = Regex.Matches(cleaned, @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();

        if (words.Count == 0)
        {
            return null;
        }

        // Join words and convert to snake_case
        string combined = string.Join(" ", words);
        return ToSnakeCase(combined);
    }

    /// <summary>
    /// Generates a tool name from an API path and HTTP method.
    /// </summary>
    /// <param name="path">The API path.</param>
    /// <param name="type">The HTTP method.</param>
    /// <returns>A snake_case tool name.</returns>
    public static string GenerateFromPathAndType(string path, HttpMethod type)
    {
        // Extract path segments, filtering out parameters
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !s.StartsWith('{') && !s.EndsWith('}'))
            .ToList();

        if (segments.Count == 0)
        {
            return ToSnakeCase(type.ToString().ToLowerInvariant());
        }

        // Determine if path has parameters
        bool hasParameters = path.Contains('{');

        // Determine action verb based on HTTP method and whether path has parameters
        string action = DetermineAction(type, hasParameters);

        // Singularize based on HTTP method and path structure:
        // - For GET with parameters: singularize the first segment
        // - For POST/PUT/PATCH/DELETE: singularize the last segment
        if (type.Method.ToUpperInvariant() == "GET" && hasParameters && segments.Count > 0)
        {
            segments[0] = Singularize(segments[0]);
        }
        else if (type.Method.ToUpperInvariant() is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            if (segments.Count > 0)
            {
                segments[^1] = Singularize(segments[^1]);
            }
        }

        // Build the tool name
        string pathPart = string.Join("_", segments);
        return ToSnakeCase($"{action}_{pathPart}");
    }

    /// <summary>
    /// Determines the action verb based on HTTP method and whether the path has parameters.
    /// </summary>
    private static string DetermineAction(HttpMethod method, bool hasParameters)
    {
        return method.Method.ToUpperInvariant() switch
        {
            "GET" => hasParameters ? "get" : "list",
            "POST" => "create",
            "PUT" => "update",
            "PATCH" => "update",
            "DELETE" => "delete",
            _ => method.ToString().ToLowerInvariant()
        };
    }

    /// <summary>
    /// Singularizes a word using basic English pluralization rules.
    /// </summary>
    private static string Singularize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return word;
        }

        word = word.ToLowerInvariant();

        // -ies → -y (categories → category)
        if (word.EndsWith("ies") && word.Length > 3)
        {
            return word[..^3] + "y";
        }

        // -ves → -f (wolves → wolf)
        if (word.EndsWith("ves") && word.Length > 3)
        {
            return word[..^3] + "f";
        }

        // -ses → -s (addresses → address)
        if (word.EndsWith("ses") && word.Length > 3)
        {
            return word[..^2];
        }

        // -xes, -ches, -shes → remove -es
        if ((word.EndsWith("xes") || word.EndsWith("ches") || word.EndsWith("shes")) && word.Length > 3)
        {
            return word[..^2];
        }

        // -s → remove (but not -ss)
        if (word.EndsWith("s") && !word.EndsWith("ss") && word.Length > 1)
        {
            return word[..^1];
        }

        return word;
    }

    /// <summary>
    /// Converts text to snake_case.
    /// </summary>
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
}
