using CommunityToolkit.Mvvm.ComponentModel;

namespace NcPasswords.App.ViewModels;

/// <summary>
/// Verifies the optional local unlock password chosen at sign-in (see <see cref="LoginViewModel"/>),
/// deriving the extra DPAPI entropy that's required - in addition to the Windows user's own DPAPI
/// key - to decrypt the data stored on this PC.
/// </summary>
public partial class UnlockViewModel(Func<string, byte[]?> tryUnlock) : ObservableObject
{
    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public byte[]? Entropy { get; private set; }

    public async Task<bool> TryUnlockAsync(string password)
    {
        ErrorMessage = null;

        if (string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Enter your password.";
            return false;
        }

        IsBusy = true;
        try
        {
            // PBKDF2 with a high iteration count is deliberately slow - keep it off the UI thread.
            var entropy = await Task.Run(() => tryUnlock(password));
            if (entropy is null)
            {
                ErrorMessage = "Incorrect password.";
                return false;
            }

            Entropy = entropy;
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
