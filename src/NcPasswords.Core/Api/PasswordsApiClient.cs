using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NcPasswords.Core.Api;

/// <summary>
/// Read-only client for the Nextcloud "Passwords" app API (server-side encryption accounts only).
/// </summary>
public sealed class PasswordsApiClient : IDisposable
{
    private const string DetailLevel = "model+folder+tags";

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private string? _sessionToken;

    public PasswordsApiClient(string serverUrl, string username, string appPassword, HttpMessageHandler? handler = null)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new ArgumentException("Server URL is required.", nameof(serverUrl));
        }

        var normalized = serverUrl.Trim().TrimEnd('/');
        var baseUri = new Uri($"{normalized}/index.php/apps/passwords/api/1.0/");

        _ownsHttpClient = true;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.BaseAddress = baseUri;
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{appPassword}")));
        _http.DefaultRequestHeaders.Add("OCS-APIRequest", "true");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Verifies the credentials and establishes a session. Throws <see cref="CseNotSupportedException"/>
    /// if the account has client-side encryption enabled, or <see cref="PasswordsAuthenticationException"/>
    /// if the credentials are rejected.
    /// </summary>
    public Task ConnectAsync(CancellationToken ct = default) => EnsureSessionAsync(ct);

    public async Task<IReadOnlyList<PasswordEntry>> ListPasswordsAsync(CancellationToken ct = default)
    {
        var entries = await PostForResultAsync<List<PasswordEntry>>(
            "password/list",
            new { details = DetailLevel },
            ct).ConfigureAwait(false);
        return entries.Where(e => !e.Trashed && !e.Hidden).ToList();
    }

    public async Task<IReadOnlyList<Folder>> ListFoldersAsync(CancellationToken ct = default)
    {
        var folders = await PostForResultAsync<List<Folder>>(
            "folder/list",
            new { details = "model" },
            ct).ConfigureAwait(false);
        return folders.Where(f => !f.Trashed).ToList();
    }

    public async Task<IReadOnlyList<Tag>> ListTagsAsync(CancellationToken ct = default)
    {
        return await PostForResultAsync<List<Tag>>(
            "tag/list",
            new { details = "model" },
            ct).ConfigureAwait(false);
    }

    private async Task<T> PostForResultAsync<T>(string relativeUrl, object body, CancellationToken ct)
    {
        await EnsureSessionAsync(ct).ConfigureAwait(false);

        var response = await SendAsync(relativeUrl, body, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Session may have expired server-side; establish a fresh one and retry once.
            _sessionToken = null;
            await EnsureSessionAsync(ct).ConfigureAwait(false);
            response = await SendAsync(relativeUrl, body, ct).ConfigureAwait(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new PasswordsApiException(
                $"Request to '{relativeUrl}' failed with status {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct).ConfigureAwait(false);
        return result ?? throw new PasswordsApiException($"Request to '{relativeUrl}' returned an empty body.");
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(body),
        };
        if (_sessionToken is not null)
        {
            request.Headers.Add("X-Api-Session", _sessionToken);
        }

        try
        {
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new PasswordsApiException($"Could not reach the server: {ex.Message}", ex);
        }
    }

    private async Task EnsureSessionAsync(CancellationToken ct)
    {
        if (_sessionToken is not null)
        {
            return;
        }

        await _sessionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_sessionToken is not null)
            {
                return;
            }

            HttpResponseMessage requestResponse;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "session/request");
                requestResponse = await _http.SendAsync(req, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new PasswordsApiException($"Could not reach the server: {ex.Message}", ex);
            }

            if (requestResponse.StatusCode == HttpStatusCode.Unauthorized ||
                requestResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new PasswordsAuthenticationException();
            }

            if (!requestResponse.IsSuccessStatusCode)
            {
                throw new PasswordsApiException(
                    $"session/request failed with status {(int)requestResponse.StatusCode} ({requestResponse.StatusCode}).");
            }

            var requestJson = await requestResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (RequiresChallenge(requestJson))
            {
                throw new CseNotSupportedException();
            }

            using var openReq = new HttpRequestMessage(HttpMethod.Post, "session/open")
            {
                Content = JsonContent.Create(new { }),
            };
            var openResponse = await _http.SendAsync(openReq, ct).ConfigureAwait(false);

            if (openResponse.StatusCode == HttpStatusCode.Unauthorized ||
                openResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new PasswordsAuthenticationException();
            }

            if (!openResponse.IsSuccessStatusCode)
            {
                throw new PasswordsApiException(
                    $"session/open failed with status {(int)openResponse.StatusCode} ({openResponse.StatusCode}).");
            }

            var token = ExtractSessionToken(openResponse);
            if (string.IsNullOrEmpty(token))
            {
                throw new PasswordsApiException("Server did not return a session token.");
            }

            _sessionToken = token;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private static bool RequiresChallenge(string sessionRequestJson)
    {
        if (string.IsNullOrWhiteSpace(sessionRequestJson))
        {
            return false;
        }

        try
        {
            var node = JsonNode.Parse(sessionRequestJson);
            if (node is not JsonObject obj)
            {
                return false;
            }

            bool HasRequirement(string key) =>
                obj.TryGetPropertyValue(key, out var value) &&
                value is not null &&
                value.GetValueKind() != JsonValueKind.Null;

            return HasRequirement("challenge") || HasRequirement("token");
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractSessionToken(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-Api-Session", out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }

    public void Dispose()
    {
        _sessionLock.Dispose();
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }
}
