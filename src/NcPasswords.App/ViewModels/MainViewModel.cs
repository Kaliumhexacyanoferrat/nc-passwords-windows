using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcPasswords.Core.Api;
using NcPasswords.Core.Organization;
using NcPasswords.Core.Storage;

namespace NcPasswords.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly TimeSpan ClipboardClearDelay = TimeSpan.FromSeconds(30);

    private readonly StoredCredentials _credentials;
    private readonly Action _onSignedOut;
    private readonly CredentialStore _credentialStore = new();
    private readonly EntryCache _cache = new();
    private PasswordsApiClient? _client;

    private List<PasswordEntry> _allPasswords = [];
    private List<Folder> _allFolders = [];
    private string? _selectedFolderId;
    private DispatcherTimer? _clipboardClearTimer;

    public MainViewModel(StoredCredentials credentials, Action onSignedOut)
    {
        _credentials = credentials;
        _onSignedOut = onSignedOut;
    }

    public ObservableCollection<PasswordEntryViewModel> Entries { get; } = [];
    public ObservableCollection<FolderNodeViewModel> Tree { get; } = [];

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private DateTimeOffset? _lastSyncedUtc;

    public async Task InitializeAsync()
    {
        var cached = _cache.Load();
        if (cached is not null)
        {
            ApplyData(cached.Passwords.ToList(), cached.Folders.ToList());
            LastSyncedUtc = cached.LastSyncedUtc;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            _client ??= new PasswordsApiClient(_credentials.ServerUrl, _credentials.Username, _credentials.AppPassword);
            await _client.ConnectAsync();

            var passwordsTask = _client.ListPasswordsAsync();
            var foldersTask = _client.ListFoldersAsync();
            await Task.WhenAll(passwordsTask, foldersTask);

            var passwords = passwordsTask.Result.ToList();
            var folders = foldersTask.Result.ToList();

            ApplyData(passwords, folders);

            LastSyncedUtc = DateTimeOffset.UtcNow;
            _cache.Save(new CachedData(passwords, folders, [], LastSyncedUtc.Value));
        }
        catch (PasswordsApiException ex)
        {
            ErrorMessage = $"Could not refresh from the server: {ex.Message}";
        }
        catch (Exception ex)
        {
            // Catch-all so an unexpected server response (or a bug in parsing it) surfaces as an
            // inline error instead of crashing the app - refreshing is routinely retried anyway.
            CrashLog.Write("RefreshAsync", ex);
            ErrorMessage = $"Could not refresh from the server: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowAllFolders()
    {
        _selectedFolderId = null;
        ApplyFilter();
    }

    public void SelectFolder(FolderNodeViewModel? node)
    {
        _selectedFolderId = node?.FolderId;
        ApplyFilter();
    }

    [RelayCommand]
    private void SignOut()
    {
        _clipboardClearTimer?.Stop();
        _credentialStore.Clear();
        _cache.Clear();
        _client?.Dispose();
        _onSignedOut();
    }

    public void CopyUsername(PasswordEntryViewModel entry) =>
        CopyToClipboard(entry.Username, $"Copied username for \"{entry.Label}\".");

    public void CopyPassword(PasswordEntryViewModel entry) =>
        CopyToClipboard(entry.Password, $"Copied password for \"{entry.Label}\" (clipboard clears in 30s).");

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyData(List<PasswordEntry> passwords, List<Folder> folders)
    {
        _allPasswords = passwords;
        _allFolders = folders;

        Tree.Clear();
        foreach (var node in FolderNodeViewModel.BuildFrom(FolderTreeBuilder.Build(folders, passwords)))
        {
            Tree.Add(node);
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IReadOnlyList<PasswordEntry> source = _selectedFolderId is null
            ? _allPasswords
            : _allPasswords.Where(p => p.Folder == _selectedFolderId).ToList();

        var filtered = EntrySearch.Filter(source, SearchText);

        Entries.Clear();
        foreach (var entry in filtered.OrderBy(e => e.Label, StringComparer.OrdinalIgnoreCase))
        {
            Entries.Add(new PasswordEntryViewModel(entry));
        }
    }

    private void CopyToClipboard(string value, string statusMessage)
    {
        try
        {
            Clipboard.SetText(value);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Another process briefly held the clipboard lock; not worth surfacing as an error.
            return;
        }

        StatusMessage = statusMessage;

        _clipboardClearTimer?.Stop();
        _clipboardClearTimer = new DispatcherTimer { Interval = ClipboardClearDelay };
        _clipboardClearTimer.Tick += (_, _) =>
        {
            _clipboardClearTimer!.Stop();
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == value)
                {
                    Clipboard.Clear();
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Clipboard busy - nothing we can do; it wasn't ours to guarantee anyway.
            }
        };
        _clipboardClearTimer.Start();
    }
}
