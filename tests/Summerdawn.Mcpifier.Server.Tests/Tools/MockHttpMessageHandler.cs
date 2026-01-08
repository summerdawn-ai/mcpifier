using System.Net;

namespace Summerdawn.Mcpifier.Server.Tests;

/// <summary>
/// Mock HttpMessageHandler for testing outbound REST calls.
/// </summary>
public class MockHttpMessageHandler(Dictionary<string, (HttpStatusCode status, string body)> responses) : HttpMessageHandler
{
    public bool WasCalled { get; private set; }

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        Requests.Add(request);

        string path = request.RequestUri?.PathAndQuery ?? "";

        foreach (var (key, (status, body)) in responses)
        {
            if (path.Contains(key))
            {
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                });
            }
        }

        // Default fallback
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }
}
