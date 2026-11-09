using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace NcPasswords.Core.Api;

/// <summary>A started browser sign-in: open <see cref="LoginUrl"/> for the user, then poll for the result.</summary>
public sealed record LoginFlow(Uri LoginUrl, Uri PollEndpoint, string PollToken);

/// <summary>What Nextcloud hands back once the user has approved access in the browser.</summary>
public sealed record LoginFlowResult(string Server, string LoginName, string AppPassword);

/// <summary>
/// Talks to Nextcloud's own authentication endpoints: Login Flow v2 (sign in through the browser and
/// receive an app password, so this app never sees the account password) and revoking that app password.
/// </summary>
public sealed class NextcloudLoginClient : IDisposable
{
    /// <summary>Nextcloud uses the User-Agent as the device name in Settings → Security, so keep it readable.</summary>
    public const string ClientName = "NcPasswords (Windows)";

    /// <summary>Nextcloud drops an unfinished login flow after 20 minutes.</summary>
    private static readonly TimeSpan FlowLifetime = TimeSpan.FromMinutes(20);

    private readonly HttpClient _http;

    public NextcloudLoginClient(HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(ClientName);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Asks the server for a new login flow (POST /login/v2).</summary>
    public async Task<LoginFlow> StartLoginFlowAsync(string serverUrl, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Normalize(serverUrl)}/index.php/login/v2");
        var response = await SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PasswordsApiException(
                $"The server did not accept the sign-in request (status {(int)response.StatusCode}). Check the server URL.");
        }

        LoginFlowResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync<LoginFlowResponse>(ct).ConfigureAwait(false);
        }
        catch (System.Text.Json.JsonException)
        {
            body = null;
        }

        if (body?.Poll is null ||
            !Uri.TryCreate(body.Login, UriKind.Absolute, out var loginUrl) ||
            !Uri.TryCreate(body.Poll.Endpoint, UriKind.Absolute, out var pollEndpoint) ||
            string.IsNullOrEmpty(body.Poll.Token))
        {
            throw new PasswordsApiException("This doesn't look like a Nextcloud server. Check the server URL.");
        }

        return new LoginFlow(loginUrl, pollEndpoint, body.Poll.Token);
    }

    /// <summary>
    /// Polls until the user has approved access in the browser, then returns the issued app password.
    /// Throws <see cref="PasswordsApiException"/> if the flow expires first.
    /// </summary>
    public async Task<LoginFlowResult> WaitForLoginAsync(LoginFlow flow, TimeSpan? pollInterval = null, CancellationToken ct = default)
    {
        var interval = pollInterval ?? TimeSpan.FromSeconds(2);
        var deadline = DateTimeOffset.UtcNow + FlowLifetime;

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Post, flow.PollEndpoint)
            {
                Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", flow.PollToken)]),
            };

            HttpResponseMessage? response;
            try
            {
                response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // A short network hiccup shouldn't abort a sign-in the user is busy approving.
                response = null;
            }

            if (response is { StatusCode: HttpStatusCode.OK })
            {
                var result = await response.Content.ReadFromJsonAsync<LoginFlowResult>(ct).ConfigureAwait(false);
                if (result is null || string.IsNullOrEmpty(result.LoginName) || string.IsNullOrEmpty(result.AppPassword))
                {
                    throw new PasswordsApiException("The server returned an incomplete sign-in result.");
                }

                return result;
            }

            // 404 means "not approved yet"; anything else we just keep retrying until the flow expires.
            await Task.Delay(interval, ct).ConfigureAwait(false);
        }

        throw new PasswordsApiException("The sign-in expired before it was approved. Please try again.");
    }

    /// <summary>Deletes the app password on the server (DELETE /ocs/v2.php/core/apppassword).</summary>
    public async Task RevokeAppPasswordAsync(string serverUrl, string username, string appPassword, CancellationToken ct = default)
    {
        using var request = OcsRequest(HttpMethod.Delete, serverUrl, "apppassword", username, appPassword);
        var response = await SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PasswordsApiException($"Revoking the app password failed with status {(int)response.StatusCode} ({response.StatusCode}).");
        }
    }

    private static HttpRequestMessage OcsRequest(HttpMethod method, string serverUrl, string endpoint, string username, string password)
    {
        var request = new HttpRequestMessage(method, $"{Normalize(serverUrl)}/ocs/v2.php/core/{endpoint}?format=json");
        request.Headers.Add("OCS-APIRequest", "true");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new PasswordsApiException($"Could not reach the server: {ex.Message}", ex);
        }
    }

    private static string Normalize(string serverUrl) => serverUrl.Trim().TrimEnd('/');

    public void Dispose() => _http.Dispose();

    private sealed record LoginFlowResponse(
        [property: JsonPropertyName("poll")] PollInfo? Poll,
        [property: JsonPropertyName("login")] string? Login);

    private sealed record PollInfo(
        [property: JsonPropertyName("token")] string? Token,
        [property: JsonPropertyName("endpoint")] string? Endpoint);
}
