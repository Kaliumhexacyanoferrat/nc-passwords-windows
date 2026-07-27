using System.Security.Cryptography;
using System.Text;

namespace NcPasswords.Core.Storage;

/// <summary>
/// Manages the optional, user-chosen password used as extra DPAPI entropy (see <see cref="DpapiProtector"/>).
/// DPAPI alone protects stored data against other Windows accounts, but not against other processes
/// running as the same account - this closes that gap for users who opt in. The password itself is
/// never written to disk, only the (non-secret) salt used to derive entropy from it via PBKDF2.
/// </summary>
public sealed class UnlockPasswordStore(string? path = null)
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 300_000;

    private readonly string _saltFile = path ?? AppPaths.UnlockSaltFile;

    public bool IsEnabled => File.Exists(_saltFile);

    /// <summary>Enables (or re-keys) the unlock password, returning the derived entropy to use immediately.</summary>
    public byte[] Enable(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        File.WriteAllBytes(_saltFile, salt);
        return Derive(password, salt);
    }

    /// <summary>Derives the entropy for a candidate password using the stored salt, or null if unlock isn't enabled.</summary>
    public byte[]? TryDerive(string password)
    {
        if (!File.Exists(_saltFile))
        {
            return null;
        }

        var salt = File.ReadAllBytes(_saltFile);
        return Derive(password, salt);
    }

    public void Clear()
    {
        if (File.Exists(_saltFile))
        {
            File.Delete(_saltFile);
        }
    }

    private static byte[] Derive(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
}
