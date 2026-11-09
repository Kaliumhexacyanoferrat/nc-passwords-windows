namespace NcPasswords.Core.Storage;

/// <param name="AppPassword">The app password Nextcloud issued to this app through the browser login flow.</param>
/// <param name="FromLoginFlow">
/// True for everything saved since the browser login flow was introduced. Versions up to 0.2.1 asked
/// for the account password and stored it here instead - such credentials are missing this flag and
/// are deleted at startup rather than used.
/// </param>
public sealed record StoredCredentials(
    string ServerUrl,
    string Username,
    string AppPassword,
    bool FromLoginFlow = false);

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

    public StoredCredentials? Load(byte[]? unlockEntropy = null)
    {
        try
        {
            return DpapiProtector.ReadProtectedJson<StoredCredentials>(_path, unlockEntropy);
        }
        catch (Exception)
        {
            // Corrupt/unreadable (e.g. moved to another machine/user, or a wrong unlock password) -
            // treat as "not logged in".
            return null;
        }
    }

    public void Save(StoredCredentials credentials, byte[]? unlockEntropy = null) =>
        DpapiProtector.WriteProtectedJson(_path, credentials, unlockEntropy);

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
