using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Summerdawn.Mcpify.Configuration;

namespace Summerdawn.Mcpify.Services;

/// <summary>
/// Service for executing REST API calls as proxy for MCP tools.
/// </summary>
public class RestProxyService(HttpClient httpClient, ILogger<RestProxyService> logger)
{
    private static readonly Regex PlaceholderRegex = new Regex(@"\{(\w+)\}");

    /// <summary>
    /// Executes a tool by making a REST API call with the specified arguments and headers.
    /// </summary>
    /// <param name="tool">The tool definition containing REST API configuration.</param>
    /// <param name="arguments">The arguments to interpolate into the REST API call.</param>
    /// <param name="forwardedHeaders">HTTP headers to forward to the REST API.</param>
    /// <returns>A tuple containing success status, HTTP status code, and response body.</returns>
    public async Task<(bool success, int statusCode, string responseBody)> ExecuteToolAsync(ProxyToolDefinition tool, Dictionary<string, JsonElement> arguments, Dictionary<string, string> forwardedHeaders)
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
        var result = query;

        var matches = PlaceholderRegex.Matches(query);

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            var paramValue = arguments.TryGetValue(paramName, out var argValue) ? argValue.ToString() : "";

            result = result.Replace($"{{{paramName}}}", paramValue);
        }

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
            var paramValue = arguments.TryGetValue(paramName, out var argValue) ? JsonSerializer.Serialize(argValue) : "null";

            result = result.Replace($"{{{paramName}}}", paramValue);
        }

        return result;
    }
}
