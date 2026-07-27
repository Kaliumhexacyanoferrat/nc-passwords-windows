namespace NcPasswords.Core.Storage;

public static class AppPaths
{
    private static readonly Lazy<string> DataDirectoryLazy = new(() =>
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NcPasswords");
        Directory.CreateDirectory(dir);
        return dir;
    });

    public static string DataDirectory => DataDirectoryLazy.Value;

    public static string CredentialsFile => Path.Combine(DataDirectory, "credentials.dat");

    public static string CacheFile => Path.Combine(DataDirectory, "cache.dat");

    /// <summary>The (non-secret) salt for the optional unlock password - see UnlockPasswordStore. Its
    /// mere existence marks the unlock password as enabled.</summary>
    public static string UnlockSaltFile => Path.Combine(DataDirectory, "unlock.salt");
}
