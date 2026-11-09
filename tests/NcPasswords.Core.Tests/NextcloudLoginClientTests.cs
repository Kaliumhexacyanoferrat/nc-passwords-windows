using System.Net;
using NcPasswords.Core.Api;
using Xunit;

namespace NcPasswords.Core.Tests;

public class NextcloudLoginClientTests
{
    private const string Server = "https://cloud.example.com";

    private const string FlowJson = """
        {"poll":{"token":"poll-token","endpoint":"https://cloud.example.com/login/v2/poll"},
         "login":"https://cloud.example.com/login/v2/flow/abc"}
        """;

    [Fact]
    public async Task StartLoginFlowAsync_ReturnsLoginUrlAndPollInfo()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("/index.php/login/v2", _ => FakeHttpMessageHandler.Json(FlowJson));

        using var client = new NextcloudLoginClient(handler);
        var flow = await client.StartLoginFlowAsync(Server + "/");

        Assert.Equal("https://cloud.example.com/login/v2/flow/abc", flow.LoginUrl.AbsoluteUri);
        Assert.Equal("https://cloud.example.com/login/v2/poll", flow.PollEndpoint.AbsoluteUri);
        Assert.Equal("poll-token", flow.PollToken);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains(NextcloudLoginClient.ClientName, request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task StartLoginFlowAsync_Throws_WhenServerIsNotNextcloud()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("/index.php/login/v2", _ => FakeHttpMessageHandler.Json("<html></html>"));

        using var client = new NextcloudLoginClient(handler);

        await Assert.ThrowsAsync<PasswordsApiException>(() => client.StartLoginFlowAsync(Server));
    }

    [Fact]
    public async Task WaitForLoginAsync_PollsUntilApproved()
    {
        var polls = 0;
        var handler = new FakeHttpMessageHandler();
        handler.On("/login/v2/poll", request =>
        {
            var body = request.Content!.ReadAsStringAsync().Result;
            Assert.Equal("token=poll-token", body);

            return ++polls < 3
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : FakeHttpMessageHandler.Json("""
                    {"server":"https://cloud.example.com","loginName":"alice","appPassword":"app-secret"}
                    """);
        });

        using var client = new NextcloudLoginClient(handler);
        var flow = new LoginFlow(new Uri(Server + "/login/v2/flow/abc"), new Uri(Server + "/login/v2/poll"), "poll-token");
        var result = await client.WaitForLoginAsync(flow, TimeSpan.Zero);

        Assert.Equal(3, polls);
        Assert.Equal("alice", result.LoginName);
        Assert.Equal("app-secret", result.AppPassword);
    }

    [Fact]
    public async Task WaitForLoginAsync_StopsWhenCanceled()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("/login/v2/poll", _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        using var client = new NextcloudLoginClient(handler);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var flow = new LoginFlow(new Uri(Server + "/login/v2/flow/abc"), new Uri(Server + "/login/v2/poll"), "poll-token");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.WaitForLoginAsync(flow, TimeSpan.FromMilliseconds(10), cts.Token));
    }

    [Fact]
    public async Task RevokeAppPasswordAsync_SendsDelete()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("/ocs/v2.php/core/apppassword", _ => FakeHttpMessageHandler.Json("""{"ocs":{"data":[]}}"""));

        using var client = new NextcloudLoginClient(handler);
        await client.RevokeAppPasswordAsync(Server, "alice", "app-secret");

        Assert.Equal(HttpMethod.Delete, Assert.Single(handler.Requests).Method);
    }
}
