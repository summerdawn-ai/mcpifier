namespace Summerdawn.Mcpifier.Services;

/// <summary>
/// A delegating handler that prepends a default query string to outgoing HTTP requests.
/// </summary>
/// <param name="defaultQuery">The default query string to prepend to outgoing HTTP requests</param>
internal sealed class DefaultQueryHandler(string? defaultQuery) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && !string.IsNullOrWhiteSpace(defaultQuery))
        {
            // Remove leading '?' or '&' from the default query string before prepending it to the request URI.
            defaultQuery = defaultQuery.TrimStart('?', '&');

            // Only prepend the default query if it has a non-zero length.
            if (defaultQuery.Length > 0)
            {
                var builder = new UriBuilder(request.RequestUri);

                // Get the query string from the request URI, removing any leading '?' character.
                string requestQuery = builder.Query.TrimStart('?');

                // Prefix the query string with the default query, ensuring proper formatting.
                builder.Query = string.IsNullOrEmpty(requestQuery) ? defaultQuery : $"{defaultQuery}&{requestQuery}";
                request.RequestUri = builder.Uri;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
