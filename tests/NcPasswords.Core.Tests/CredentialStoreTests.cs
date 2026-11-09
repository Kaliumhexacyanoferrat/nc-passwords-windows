using NcPasswords.Core.Storage;
using Xunit;

namespace NcPasswords.Core.Tests;

public class CredentialStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"ncpasswords-test-{Guid.NewGuid():N}.bin");

    [Fact]
    public void Load_FlagsCredentialsSavedByOlderVersions_AsNotFromLoginFlow()
    {
        // Versions up to 0.2.1 stored only these three fields.
        DpapiProtector.WriteProtectedJson(_path, new { ServerUrl = "https://cloud.example.com", Username = "alice", AppPassword = "secret" });

        var loaded = new CredentialStore(_path).Load();

        Assert.NotNull(loaded);
        Assert.Equal("secret", loaded.AppPassword);
        Assert.False(loaded.FromLoginFlow);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsLoginFlowFlag()
    {
        var store = new CredentialStore(_path);
        store.Save(new StoredCredentials("https://cloud.example.com", "alice", "app-secret", FromLoginFlow: true));

        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.True(loaded.FromLoginFlow);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
