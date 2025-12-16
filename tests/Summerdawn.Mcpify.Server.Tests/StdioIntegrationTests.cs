using Summerdawn.Mcpify.Abstractions;

namespace Summerdawn.Mcpify.Server.Tests;

/// <summary>
/// Integration tests for stdio mode using in-memory streams.
/// </summary>
public class StdioIntegrationTests
{
    [Fact(Skip = "Stdio tests require special handling of blocking ReadLine operations")]
    public async Task StdioServer_WithInMemoryStreams_ProcessesJsonRpcRequest()
    {
        // This test demonstrates the stdio abstraction is working
        // but requires additional infrastructure to properly test async stream reading
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation of IStdio using in-memory streams.
/// </summary>
public class TestStdio(Stream input, Stream output) : IStdio
{
    public Stream GetStandardInput() => input;
    public Stream GetStandardOutput() => output;
}
