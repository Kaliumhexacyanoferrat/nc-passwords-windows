using System.Net;
using NcPasswords.Core.Api;
using Xunit;

namespace NcPasswords.Core.Tests;

public class PasswordsApiClientTests
{
    private const string NoCseSessionRequest = """{"success":true,"challenge":null,"token":null}""";
    private const string CseSessionRequest = """{"success":true,"challenge":{"type":"PBKDF2"},"token":null}""";

    private static HttpResponseMessage SessionOpenResponse(string token)
    {
        var response = FakeHttpMessageHandler.Json("{}");
        response.Headers.Add("X-Api-Session", token);
        return response;
    }

    [Fact]
    public async Task ListPasswordsAsync_EstablishesSessionAndReturnsEntries_WhenNoCseRequired()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("session/request", _ => FakeHttpMessageHandler.Json(NoCseSessionRequest));
        handler.On("session/open", _ => SessionOpenResponse("token-abc"));
        handler.On("password/list", _ => FakeHttpMessageHandler.Json("""
            [{"id":"1","label":"Test Entry","username":"alice","password":"hunter2","url":"https://example.com",
              "notes":"","customFields":"","folder":"00000000-0000-0000-0000-000000000000","tags":[],
              "favorite":false,"trashed":false,"hidden":false,"created":0,"updated":0,"edited":0}]
            """));

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "pass", handler);
        var result = await client.ListPasswordsAsync();

        Assert.Single(result);
        Assert.Equal("Test Entry", result[0].Label);
        Assert.Equal("alice", result[0].Username);

        var listRequest = handler.Requests.Single(r => r.RequestUri!.AbsolutePath.EndsWith("password/list"));
        Assert.Equal("token-abc", listRequest.Headers.GetValues("X-Api-Session").Single());
    }

    [Fact]
    public async Task ListPasswordsAsync_ExtractsFolderId_WhenServerExpandsFolderToNestedObject()
    {
        // When "folder" is included in the requested details, the server replaces the plain
        // folder id string with a full nested folder object instead.
        var handler = new FakeHttpMessageHandler();
        handler.On("session/request", _ => FakeHttpMessageHandler.Json(NoCseSessionRequest));
        handler.On("session/open", _ => SessionOpenResponse("token-abc"));
        handler.On("password/list", _ => FakeHttpMessageHandler.Json("""
            [{"id":"1","label":"Test Entry","username":"alice","password":"hunter2","url":"https://example.com",
              "notes":"","customFields":"","folder":{"id":"f1","label":"Work","parent":"00000000-0000-0000-0000-000000000000"},
              "tags":[],"favorite":false,"trashed":false,"hidden":false,"created":0,"updated":0,"edited":0}]
            """));

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "pass", handler);
        var result = await client.ListPasswordsAsync();

        Assert.Equal("f1", Assert.Single(result).Folder);
    }

    [Fact]
    public async Task ListPasswordsAsync_ToleratesNonStringCustomFields()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("session/request", _ => FakeHttpMessageHandler.Json(NoCseSessionRequest));
        handler.On("session/open", _ => SessionOpenResponse("token-abc"));
        handler.On("password/list", _ => FakeHttpMessageHandler.Json("""
            [{"id":"1","label":"Test Entry","username":"alice","password":"hunter2","url":"https://example.com",
              "notes":"","customFields":[{"label":"PIN","type":"text","value":"1234"}],"folder":"00000000-0000-0000-0000-000000000000",
              "tags":[],"favorite":false,"trashed":false,"hidden":false,"created":0,"updated":0,"edited":0}]
            """));

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "pass", handler);
        var result = await client.ListPasswordsAsync();

        var fields = CustomFieldParser.Parse(Assert.Single(result).CustomFields);
        Assert.Equal("PIN", Assert.Single(fields).Label);
    }

    [Fact]
    public async Task ConnectAsync_ThrowsCseNotSupported_WhenAccountRequiresChallenge()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("session/request", _ => FakeHttpMessageHandler.Json(CseSessionRequest));

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "pass", handler);

        await Assert.ThrowsAsync<CseNotSupportedException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_ThrowsAuthenticationException_WhenCredentialsRejected()
    {
        var handler = new FakeHttpMessageHandler();
        handler.On("session/request", _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "wrong", handler);

        await Assert.ThrowsAsync<PasswordsAuthenticationException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task ListPasswordsAsync_ReestablishesSessionAndRetriesOnce_WhenSessionExpired()
    {
        var handler = new FakeHttpMessageHandler();
        var sessionRequestCalls = 0;
        var listCalls = 0;

        handler.On("session/request", _ =>
        {
            sessionRequestCalls++;
            return FakeHttpMessageHandler.Json(NoCseSessionRequest);
        });
        handler.On("session/open", _ => SessionOpenResponse($"token-{sessionRequestCalls}"));
        handler.On("password/list", _ =>
        {
            listCalls++;
            return listCalls == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : FakeHttpMessageHandler.Json("[]");
        });

        using var client = new PasswordsApiClient("https://cloud.example.com", "user", "pass", handler);
        var result = await client.ListPasswordsAsync();

        Assert.Empty(result);
        Assert.Equal(2, sessionRequestCalls);
        Assert.Equal(2, listCalls);
    }
}
