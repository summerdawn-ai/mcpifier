using System.Text.Json.Serialization;

namespace Summerdawn.Mcpifier.Models;

/// <summary>
/// Represents the resource metadata for OAuth authorization as defined in RFC 9396.
/// Defined by <see href="https://datatracker.ietf.org/doc/rfc9728/">RFC 9728</see>.
/// </summary>
public sealed class ProtectedResourceMetadata
{
    /// <summary>
    /// The resource URI.
    /// </summary>
    /// <remarks>
    /// REQUIRED. The protected resource's resource identifier.
    /// </remarks>
    [JsonPropertyName("resource")]
    public Uri Resource { get; set; } = null!;

    /// <summary>
    /// The list of authorization server URIs.
    /// </summary>
    /// <remarks>
    /// OPTIONAL. JSON array containing a list of OAuth authorization server issuer identifiers
    /// for authorization servers that can be used with this protected resource.
    /// </remarks>
    [JsonPropertyName("authorization_servers")]
    public List<Uri> AuthorizationServers { get; set; } = [];

    /// <summary>
    /// The supported bearer token methods.
    /// </summary>
    /// <remarks>
    /// OPTIONAL. JSON array containing a list of the supported methods of sending an OAuth 2.0 bearer token
    /// to the protected resource. Defined values are ["header", "body", "query"].
    /// </remarks>
    [JsonPropertyName("bearer_methods_supported")]
    public List<string> BearerMethodsSupported { get; set; } = ["header"];

    /// <summary>
    /// The supported scopes.
    /// </summary>
    /// <remarks>
    /// RECOMMENDED. JSON array containing a list of scope values that are used in authorization
    /// requests to request access to this protected resource.
    /// </remarks>
    [JsonPropertyName("scopes_supported")]
    public List<string> ScopesSupported { get; set; } = [];
}
