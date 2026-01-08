namespace Summerdawn.Mcpifier.Tests;

/// <summary>
/// Mock HttpMessageHandler for testing outbound REST calls.
/// </summary>
public class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public bool WasCalled { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return await handler(request, cancellationToken);
    }
}
