namespace NcPasswords.Core.Storage;

public sealed record StoredCredentials(string ServerUrl, string Username, string AppPassword);

/// <summary>
/// Persists the Nextcloud connection details to disk, DPAPI-encrypted for the current
/// Windows user (see CLAUDE.md: "Store the connection information encrypted in a
/// windows specific mechanism").
/// </summary>
public sealed class CredentialStore
{
    private readonly string _path;

    public CredentialStore(string? path = null)
    {
        _path = path ?? AppPaths.CredentialsFile;
    }

    public bool Exists => File.Exists(_path);

    public StoredCredentials? Load()
    {
        try
        {
            return DpapiProtector.ReadProtectedJson<StoredCredentials>(_path);
        }
        catch (Exception)
        {
            // Corrupt/unreadable (e.g. moved to another machine/user) - treat as "not logged in".
            return null;
        }
    }

    public void Save(StoredCredentials credentials) =>
        DpapiProtector.WriteProtectedJson(_path, credentials);

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
