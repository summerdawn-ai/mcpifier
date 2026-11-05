using System.Net;
using System.Net.Sockets;

namespace Summerdawn.Mcpifier.Server;

/// <summary>
/// Selects a port for a Server host.
/// </summary>
internal static class ServerPortSelector
{
    private const int DefaultPort = 5157;

    /// <summary>
    /// Returns the requested port or the first available port at or above the default.
    /// </summary>
    /// <param name="requestedPort">The explicitly requested port, if any.</param>
    /// <returns>The selected port.</returns>
    internal static int Select(int? requestedPort)
    {
        if (requestedPort is int port)
        {
            ValidatePort(port);
            return port;
        }

        for (int candidate = DefaultPort; candidate <= IPEndPoint.MaxPort; candidate++)
        {
            if (IsAvailable(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No available Server port was found starting at {DefaultPort}.");
    }

    private static bool IsAvailable(int port)
    {
        using var listener = new TcpListener(IPAddress.Loopback, port);
        try
        {
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static void ValidatePort(int port)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "The Server port must be between 0 and 65535.");
        }
    }
}
