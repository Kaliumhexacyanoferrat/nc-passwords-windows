using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcPasswords.Core.Api;
using NcPasswords.Core.Storage;

namespace NcPasswords.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly Action<StoredCredentials, byte[]?> _onLoggedIn;
    private readonly CredentialStore _credentialStore = new();
    private readonly UnlockPasswordStore _unlockPasswordStore = new();

    public LoginViewModel(Action<StoredCredentials, byte[]?> onLoggedIn)
    {
        _onLoggedIn = onLoggedIn;
    }

    [ObservableProperty]
    private string _serverUrl = "";

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public async Task LoginAsync(string password, string? unlockPassword = null)
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(ServerUrl) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Server URL, username and password are all required.";
            return;
        }

        if (!Uri.TryCreate(ServerUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ErrorMessage = "Enter a valid server URL, e.g. https://cloud.example.com";
            return;
        }

        IsBusy = true;
        try
        {
            using var client = new PasswordsApiClient(ServerUrl.Trim(), Username.Trim(), password);
            await client.ConnectAsync();

            // Deriving the unlock entropy runs PBKDF2 with a high iteration count - keep it off the UI thread.
            var unlockEntropy = string.IsNullOrEmpty(unlockPassword)
                ? null
                : await Task.Run(() => _unlockPasswordStore.Enable(unlockPassword));

            if (unlockEntropy is null)
            {
                _unlockPasswordStore.Clear();
            }

            var credentials = new StoredCredentials(ServerUrl.Trim(), Username.Trim(), password);
            _credentialStore.Save(credentials, unlockEntropy);
            _onLoggedIn(credentials, unlockEntropy);
        }
        catch (CseNotSupportedException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (PasswordsAuthenticationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (PasswordsApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
