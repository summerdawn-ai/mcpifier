using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpify.Services;

namespace Summerdawn.Mcpify.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="McpifyBuilder"/>.
/// </summary>
public static class McpifyBuilderExtensions
{
    /// <summary>
    /// Adds ASP.NET Core route handler and related services to Mcpify.
    /// </summary>
    public static McpifyBuilder AddAspNetCore(this McpifyBuilder mcpifyBuilder)
    {
        var services = mcpifyBuilder.Services;

        // Add ASP.NET Core's server address to support relative URI as Mcpify base address.
        services.AddKeyedSingleton<Uri>("Mcpify:ServerAddress", (provider, _) => GetServerAddress(provider)!);

        // Add context accessor for forwarding HTTP headers.
        services.AddHttpContextAccessor();

        // Add ASP.NET Core HTTP routing handler.
        services.AddSingleton<McpRouteHandler>();

        return mcpifyBuilder;
    }

    /// <summary>
    /// Gets the (first) address of the ASP.NET Core (Kestrel) server.
    /// </summary>
    private static Uri? GetServerAddress(IServiceProvider provider)
    {
        var serverFeatures = provider.GetService<IServer>();
        var addressesFeature = serverFeatures?.Features.Get<IServerAddressesFeature>();
        var serverAddress = addressesFeature?.Addresses.FirstOrDefault();

        return Uri.TryCreate(serverAddress, UriKind.Absolute, out var uri) ? uri : null;
    }
}