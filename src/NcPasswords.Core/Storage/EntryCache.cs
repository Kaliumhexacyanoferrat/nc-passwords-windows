using NcPasswords.Core.Api;

namespace NcPasswords.Core.Storage;

public sealed record CachedData(
    IReadOnlyList<PasswordEntry> Passwords,
    IReadOnlyList<Folder> Folders,
    IReadOnlyList<Tag> Tags,
    DateTimeOffset LastSyncedUtc);

/// <summary>
/// Local, DPAPI-encrypted cache of the last-synced entries so the UI has something to show
/// instantly on launch, before/without a network round-trip.
/// </summary>
public sealed class EntryCache
{
    private readonly string _path;

    public EntryCache(string? path = null)
    {
        _path = path ?? AppPaths.CacheFile;
    }

    public CachedData? Load()
    {
        try
        {
            return DpapiProtector.ReadProtectedJson<CachedData>(_path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Save(CachedData data) => DpapiProtector.WriteProtectedJson(_path, data);

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
