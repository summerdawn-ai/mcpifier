using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

using Summerdawn.Mcpifier.Services;

namespace Summerdawn.Mcpifier.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="McpifierBuilder"/>.
/// </summary>
public static class McpifierBuilderExtensions
{
    /// <summary>
    /// Adds ASP.NET Core route handler and related services to Mcpifier.
    /// </summary>
    public static McpifierBuilder AddAspNetCore(this McpifierBuilder mcpifierBuilder)
    {
        var services = mcpifierBuilder.Services;

        // Add ASP.NET Core's server address to support relative URI as Mcpifier base address.
        services.AddKeyedSingleton<Uri>("Mcpifier:ServerAddress", (provider, _) => GetServerAddress(provider)!);

        // Add context accessor for forwarding HTTP headers.
        services.AddHttpContextAccessor();

        // Add ASP.NET Core HTTP routing handler.
        services.AddSingleton<McpRouteHandler>();

        return mcpifierBuilder;
    }

    /// <summary>
    /// Gets the (first) address of the ASP.NET Core (Kestrel) server.
    /// </summary>
    /// <remarks>
    /// Note that the list of addresses will be empty until _after_ the server has started!
    /// </remarks>
    private static Uri? GetServerAddress(IServiceProvider provider)
    {
        var serverFeatures = provider.GetService<IServer>();
        var addressesFeature = serverFeatures?.Features.Get<IServerAddressesFeature>();
        string? serverAddress = addressesFeature?.Addresses.FirstOrDefault();

        return Uri.TryCreate(serverAddress, UriKind.Absolute, out var uri) ? uri : null;
    }
}
