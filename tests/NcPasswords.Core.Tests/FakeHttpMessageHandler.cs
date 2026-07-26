using System.Net;

namespace NcPasswords.Core.Tests;

/// <summary>Routes requests to a per-relative-path responder, so tests can script a fake Passwords server.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();
    public List<HttpRequestMessage> Requests { get; } = [];

    public void On(string pathAndQueryOrPath, Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responders[pathAndQueryOrPath] = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var path = request.RequestUri!.AbsolutePath;
        var key = path[(path.IndexOf("/api/1.0/", StringComparison.Ordinal) + "/api/1.0/".Length)..];

        if (_responders.TryGetValue(key, out var responder))
        {
            return Task.FromResult(responder(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
        return response;
    }
}
