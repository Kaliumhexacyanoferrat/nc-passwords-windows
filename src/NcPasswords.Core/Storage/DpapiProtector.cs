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
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("NcPasswords.v1");

    public static byte[] Protect(byte[] plainBytes) =>
        ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

    public static byte[] Unprotect(byte[] protectedBytes) =>
        ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);

    public static void WriteProtectedJson<T>(string path, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var protectedBytes = Protect(Encoding.UTF8.GetBytes(json));
        File.WriteAllBytes(path, protectedBytes);
    }

    public static T? ReadProtectedJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var protectedBytes = File.ReadAllBytes(path);
        var plainBytes = Unprotect(protectedBytes);
        var json = Encoding.UTF8.GetString(plainBytes);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }
}
