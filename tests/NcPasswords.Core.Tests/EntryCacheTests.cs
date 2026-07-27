using NcPasswords.Core.Api;
using NcPasswords.Core.Storage;
using Xunit;

namespace NcPasswords.Core.Tests;

public class EntryCacheTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"nc-passwords-test-{Guid.NewGuid():N}.dat");

    [Fact]
    public void SaveThenLoad_RoundTripsData()
    {
        var cache = new EntryCache(_tempFile);
        var entry = new PasswordEntry { Id = "1", Label = "Example", Username = "bob", Password = "s3cret" };
        var folder = new Folder { Id = "f1", Label = "Work" };
        var syncedAt = DateTimeOffset.UtcNow;

        cache.Save(new CachedData([entry], [folder], [], syncedAt));
        var loaded = cache.Load();

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Passwords);
        Assert.Equal("Example", loaded.Passwords[0].Label);
        Assert.Equal("s3cret", loaded.Passwords[0].Password);
        Assert.Single(loaded.Folders);
        Assert.Equal("Work", loaded.Folders[0].Label);
        Assert.Equal(syncedAt, loaded.LastSyncedUtc);
    }

    [Fact]
    public void Load_ReturnsNull_WhenNoCacheFileExists()
    {
        var cache = new EntryCache(_tempFile);
        Assert.Null(cache.Load());
    }

    [Fact]
    public void SaveThenLoad_WithUnlockEntropy_RoundTripsData()
    {
        var cache = new EntryCache(_tempFile);
        var entropy = new byte[] { 1, 2, 3, 4 };
        var entry = new PasswordEntry { Id = "1", Label = "Example", Username = "bob", Password = "s3cret" };

        cache.Save(new CachedData([entry], [], [], DateTimeOffset.UtcNow), entropy);
        var loaded = cache.Load(entropy);

        Assert.NotNull(loaded);
        Assert.Equal("s3cret", loaded!.Passwords[0].Password);
    }

    [Fact]
    public void Load_ReturnsNull_WhenUnlockEntropyIsWrong()
    {
        var cache = new EntryCache(_tempFile);
        cache.Save(new CachedData([], [], [], DateTimeOffset.UtcNow), [1, 2, 3, 4]);

        var loaded = cache.Load([9, 9, 9, 9]);

        Assert.Null(loaded);
    }

    [Fact]
    public void Clear_RemovesCacheFile()
    {
        var cache = new EntryCache(_tempFile);
        cache.Save(new CachedData([], [], [], DateTimeOffset.UtcNow));
        Assert.True(File.Exists(_tempFile));

        cache.Clear();

        Assert.False(File.Exists(_tempFile));
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }
}
