using System.Net;

namespace NcPasswords.Core.Api;

/// <summary>
/// Creates the HTTP handler used to reach Nextcloud: requests go through the proxy configured in Windows
/// (Settings → Network &amp; internet → Proxy, including setup scripts and automatic detection, or the
/// HTTP_PROXY / HTTPS_PROXY environment variables), and a proxy that asks for authentication is answered
/// with the signed-in Windows account, as browsers do in a company network.
/// </summary>
internal static class SystemProxy
{
    public static HttpMessageHandler CreateHandler() => new SocketsHttpHandler
    {
        UseProxy = true,
        Proxy = HttpClient.DefaultProxy,
        DefaultProxyCredentials = CredentialCache.DefaultCredentials,
    };
}
