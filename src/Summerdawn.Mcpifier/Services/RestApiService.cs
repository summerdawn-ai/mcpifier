using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Summerdawn.Mcpifier.Configuration;

namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// Service for executing MCP tools as REST API calls.
/// </summary>
public class RestApiService(HttpClient httpClient, ILogger<RestApiService> logger)
{
    /// <summary>
    /// Defines a pattern for a placeholder in the form "{argumentName}".
    /// </summary>
    private static readonly Regex PlaceholderRegex = new Regex(@"\{([^{}]+)\}");

    /// <summary>
    /// Defines a pattern for a query parameter assignment in the form "param={argumentName}".
    /// </summary>
    private static readonly Regex PlaceholderAssignmentRegex = new Regex(@"([^&?]*?)=\{([^}]+)\}");

    /// <summary>
    /// Executes a tool by making a REST API call with the specified arguments and headers.
    /// </summary>
    /// <param name="tool">The tool definition containing REST API configuration.</param>
    /// <param name="arguments">The arguments to interpolate into the REST API call.</param>
    /// <param name="forwardedHeaders">HTTP headers to forward to the REST API.</param>
    /// <returns>A tuple containing success status, HTTP status code, and response body.</returns>
    public async Task<(bool success, int statusCode, string responseBody)> ExecuteToolAsync(McpifierToolMapping tool, Dictionary<string, JsonElement> arguments, Dictionary<string, string> forwardedHeaders)
    {
        // Build the URL with path interpolation
        var path = InterpolatePath(tool.Rest.Path, arguments);

        // Add query parameters
        if (tool.Rest.Query is not null)
        {
            string queryString = InterpolateQuery(tool.Rest.Query, arguments);
            path = $"{path}?{queryString}";
        }

        // Make sure the path is relative to the base address even if the REST path has a leading "/".
        // Swagger uses leading slashes in paths, but they mess with URL composition.
        if (path.StartsWith('/')) path = path[1..];

        logger.LogInformation("Executing tool {ToolName}: {Method} {Path}", tool.Mcp.Name, tool.Rest.Method, path);

        // Create the HTTP request
        var request = new HttpRequestMessage(new HttpMethod(tool.Rest.Method), path);

        // Forward headers
        foreach (var (headerName, headerValue) in forwardedHeaders)
        {
            logger.LogDebug("Forwarding header {HeaderName}", headerName);
            request.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        // Add body if present
        if (tool.Rest.Body != null)
        {
            var body = InterpolateBody(tool.Rest.Body, arguments);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        // Execute the request
        try
        {
            var response = await httpClient.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync();

            logger.LogInformation("REST API response: {StatusCode} for tool {ToolName}", statusCode, tool.Mcp.Name);

            return (response.IsSuccessStatusCode, statusCode, responseBody);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed for tool {ToolName}", tool.Mcp.Name);
            return (false, 500, $"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing tool {ToolName}", tool.Mcp.Name);
            return (false, 500, $"Unexpected error: {ex.Message}");
        }
    }

    private static string InterpolatePath(string path, Dictionary<string, JsonElement> arguments)
    {
        var result = path;
        var matches = PlaceholderRegex.Matches(path);

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            var paramValue = arguments.TryGetValue(paramName, out var argValue) ? Uri.EscapeDataString(argValue.ToString()) : "";

            result = result.Replace($"{{{paramName}}}", paramValue);
        }

        return result;
    }

    private static string InterpolateQuery(string query, Dictionary<string, JsonElement> arguments)
    {
        // First, remove query parameters where the argument is not provided
        string cleanedQuery = RemoveAbsentArguments(query, arguments);
        
        // Then interpolate the remaining parameters
        var result = cleanedQuery;
        var matches = PlaceholderRegex.Matches(cleanedQuery);

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            var paramValue = arguments.TryGetValue(paramName, out var argValue) ? Uri.EscapeDataString(argValue.ToString()) : "";

            result = result.Replace($"{{{paramName}}}", paramValue);
        }

        return result;
    }

    /// <summary>
    /// Removes query parameter assignments where the corresponding argument is not provided.
    /// </summary>
    /// <remarks>
    /// Works by identifying param=argument patterns and removing the entire assignment.
    /// If the argument name is not present in the arguments dictionary, the parameter is removed.
    /// For example: "from={from}&amp;to={to}" with arguments containing only "from"
    /// becomes "from={from}".
    /// </remarks>
    /// <param name="query">The original query string with placeholders.</param>
    /// <param name="arguments">The arguments dictionary.</param>
    /// <returns>Query string with unsupported parameters removed.</returns>
    private static string RemoveAbsentArguments(string query, Dictionary<string, JsonElement> arguments)
    {
        var result = query;
        var matches = PlaceholderAssignmentRegex.Matches(query);
        
        // Process matches in reverse to maintain correct indices when removing
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            var argName = match.Groups[2].Value;
            
            // If the argument doesn't exist, remove the entire parameter assignment
            if (!arguments.ContainsKey(argName))
            {
                result = result.Remove(match.Index, match.Length);
            }
        }
        
        // Cleanup: remove orphaned & characters
        // Replace multiple consecutive & with single &
        result = Regex.Replace(result, "&+", "&");
        // Remove leading &
        result = result.TrimStart('&');
        // Remove trailing &
        result = result.TrimEnd('&');
        
        return result;
    }

    private static string InterpolateBody(string body, Dictionary<string, JsonElement> arguments)
    {
        string result = body;

        // Replace placeholders in the JSON string
        var matches = PlaceholderRegex.Matches(body);

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            var paramValue = arguments.TryGetValue(paramName, out var argValue) ? argValue.GetRawText() : "null";

            result = result.Replace($"{{{paramName}}}", paramValue);
        }

        return result;
    }
}
