using System.Security.Cryptography;
using System.Text;

namespace NcPasswords.Core.Storage;

/// <summary>
/// Encrypts/decrypts data for the current Windows user using DPAPI, so secrets on disk
/// are unreadable outside this Windows account without any extra key management.
/// </summary>
public static class DpapiProtector
{
    // Binds the encrypted blob to this app specifically, on top of the per-user DPAPI key.
    private static readonly byte[] BaseEntropy = Encoding.UTF8.GetBytes("NcPasswords.v1");

    public static byte[] Protect(byte[] plainBytes, byte[]? extraEntropy = null) =>
        ProtectedData.Protect(plainBytes, CombineEntropy(extraEntropy), DataProtectionScope.CurrentUser);

    public static byte[] Unprotect(byte[] protectedBytes, byte[]? extraEntropy = null) =>
        ProtectedData.Unprotect(protectedBytes, CombineEntropy(extraEntropy), DataProtectionScope.CurrentUser);

    public static void WriteProtectedJson<T>(string path, T value, byte[]? extraEntropy = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var protectedBytes = Protect(Encoding.UTF8.GetBytes(json), extraEntropy);
        File.WriteAllBytes(path, protectedBytes);
    }

    public static T? ReadProtectedJson<T>(string path, byte[]? extraEntropy = null)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var protectedBytes = File.ReadAllBytes(path);
        var plainBytes = Unprotect(protectedBytes, extraEntropy);
        var json = Encoding.UTF8.GetString(plainBytes);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }

    // extraEntropy is the (optional) key derived from a user-chosen unlock password - see
    // UnlockPasswordStore. Folding it into the DPAPI entropy means decrypting the file requires
    // both the Windows user's own DPAPI key *and* that password, instead of the DPAPI key alone.
    private static byte[] CombineEntropy(byte[]? extraEntropy) =>
        extraEntropy is null or { Length: 0 } ? BaseEntropy : [.. BaseEntropy, .. extraEntropy];
}
