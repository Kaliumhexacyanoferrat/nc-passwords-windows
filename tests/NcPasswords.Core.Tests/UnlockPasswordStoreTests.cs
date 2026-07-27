using NcPasswords.Core.Storage;
using Xunit;

namespace NcPasswords.Core.Tests;

public class UnlockPasswordStoreTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"nc-passwords-test-{Guid.NewGuid():N}.salt");

    [Fact]
    public void IsEnabled_FalseUntilEnabled()
    {
        var store = new UnlockPasswordStore(_tempFile);
        Assert.False(store.IsEnabled);

        store.Enable("correct horse battery staple");

        Assert.True(store.IsEnabled);
    }

    [Fact]
    public void TryDerive_ReturnsSameBytes_ForSamePassword()
    {
        var store = new UnlockPasswordStore(_tempFile);
        var enabled = store.Enable("hunter2");

        var derived = store.TryDerive("hunter2");

        Assert.NotNull(derived);
        Assert.Equal(enabled, derived);
    }

    [Fact]
    public void TryDerive_ReturnsDifferentBytes_ForWrongPassword()
    {
        var store = new UnlockPasswordStore(_tempFile);
        var enabled = store.Enable("hunter2");

        var derived = store.TryDerive("wrong-password");

        Assert.NotNull(derived);
        Assert.NotEqual(enabled, derived);
    }

    [Fact]
    public void TryDerive_ReturnsNull_WhenNotEnabled()
    {
        var store = new UnlockPasswordStore(_tempFile);
        Assert.Null(store.TryDerive("anything"));
    }

    [Fact]
    public void Clear_DisablesAndRemovesFile()
    {
        var store = new UnlockPasswordStore(_tempFile);
        store.Enable("hunter2");
        Assert.True(File.Exists(_tempFile));

        store.Clear();

        Assert.False(store.IsEnabled);
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
