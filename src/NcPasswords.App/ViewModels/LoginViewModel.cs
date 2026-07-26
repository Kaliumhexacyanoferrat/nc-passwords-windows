using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcPasswords.Core.Api;
using NcPasswords.Core.Storage;

namespace NcPasswords.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly Action<StoredCredentials> _onLoggedIn;
    private readonly CredentialStore _credentialStore = new();

    public LoginViewModel(Action<StoredCredentials> onLoggedIn)
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

    public async Task LoginAsync(string password)
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

            var credentials = new StoredCredentials(ServerUrl.Trim(), Username.Trim(), password);
            _credentialStore.Save(credentials);
            _onLoggedIn(credentials);
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
