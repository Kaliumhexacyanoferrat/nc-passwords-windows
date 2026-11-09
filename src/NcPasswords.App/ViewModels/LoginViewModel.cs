using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcPasswords.Core.Api;
using NcPasswords.Core.Storage;

namespace NcPasswords.App.ViewModels;

/// <summary>
/// Signs in through Nextcloud's browser login flow: the user approves access on their own Nextcloud
/// page, and the server hands this app an app password - the account password never passes through here.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly Action<StoredCredentials, byte[]?> _onLoggedIn;
    private readonly CredentialStore _credentialStore = new();
    private readonly UnlockPasswordStore _unlockPasswordStore = new();
    private CancellationTokenSource? _flowCancellation;
    private Uri? _loginUrl;

    public LoginViewModel(Action<StoredCredentials, byte[]?> onLoggedIn, string? serverUrl = null, string? infoMessage = null)
    {
        _onLoggedIn = onLoggedIn;
        _serverUrl = serverUrl ?? "";
        _infoMessage = infoMessage;
    }

    [ObservableProperty]
    private string _serverUrl = "";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>True while the browser is open and we're waiting for the user to grant access.</summary>
    [ObservableProperty]
    private bool _isWaitingForBrowser;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _infoMessage;

    public async Task LoginAsync(string? unlockPassword = null)
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        InfoMessage = null;

        if (!Uri.TryCreate(ServerUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ErrorMessage = "Enter a valid server URL, e.g. https://cloud.example.com";
            return;
        }

        var serverUrl = ServerUrl.Trim().TrimEnd('/');

        IsBusy = true;
        _flowCancellation = new CancellationTokenSource();
        var ct = _flowCancellation.Token;
        try
        {
            using var loginClient = new NextcloudLoginClient();

            var flow = await loginClient.StartLoginFlowAsync(serverUrl, ct);
            _loginUrl = flow.LoginUrl;
            IsWaitingForBrowser = true;
            OpenBrowser();

            var result = await loginClient.WaitForLoginAsync(flow, ct: ct);
            IsWaitingForBrowser = false;

            // Keep the address the user typed: it's the one known to work from this PC, while the
            // one the server reports can be an internal name behind a reverse proxy.
            var credentials = new StoredCredentials(serverUrl, result.LoginName, result.AppPassword, FromLoginFlow: true);

            try
            {
                using var client = new PasswordsApiClient(credentials.ServerUrl, credentials.Username, credentials.AppPassword);
                await client.ConnectAsync(ct);
            }
            catch
            {
                // Don't leave an unused app password behind on the server.
                _ = RevokeQuietlyAsync(credentials);
                throw;
            }

            // Deriving the unlock entropy runs PBKDF2 with a high iteration count - keep it off the UI thread.
            var unlockEntropy = string.IsNullOrEmpty(unlockPassword)
                ? null
                : await Task.Run(() => _unlockPasswordStore.Enable(unlockPassword));

            if (unlockEntropy is null)
            {
                _unlockPasswordStore.Clear();
            }

            _credentialStore.Save(credentials, unlockEntropy);
            _onLoggedIn(credentials, unlockEntropy);
        }
        catch (OperationCanceledException)
        {
            // User clicked Cancel or closed the window.
        }
        catch (PasswordsApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsWaitingForBrowser = false;
            IsBusy = false;
            _loginUrl = null;
            _flowCancellation.Dispose();
            _flowCancellation = null;
        }
    }

    [RelayCommand]
    public void Cancel() => _flowCancellation?.Cancel();

    /// <summary>Opens the Nextcloud approval page in the default browser (again, if the user closed it).</summary>
    [RelayCommand]
    private void OpenBrowser()
    {
        if (_loginUrl is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_loginUrl.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            CrashLog.Write("OpenBrowser", ex);
            ErrorMessage = $"Could not open your browser. Open this address manually: {_loginUrl.AbsoluteUri}";
        }
    }

    internal static async Task RevokeQuietlyAsync(StoredCredentials credentials)
    {
        try
        {
            using var loginClient = new NextcloudLoginClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await loginClient.RevokeAppPasswordAsync(credentials.ServerUrl, credentials.Username, credentials.AppPassword, timeout.Token);
        }
        catch (Exception)
        {
            // Best effort - the user can still remove it under Settings → Security in Nextcloud.
        }
    }
}
