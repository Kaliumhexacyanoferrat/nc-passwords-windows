using System.IO;
using NcPasswords.Core.Storage;

namespace NcPasswords.App;

/// <summary>
/// Appends diagnostics to a file on disk. Exists because a process can die before a MessageBox
/// ever gets a chance to render (e.g. an exception on a background thread, or a double-clicked
/// WinExe with no attached console to print to) - a log on disk survives that either way.
/// </summary>
public static class CrashLog
{
    private static readonly string Path = System.IO.Path.Combine(AppPaths.DataDirectory, "crash.log");

    public static void Write(string source, Exception ex)
    {
        try
        {
            File.AppendAllText(Path, $"[{DateTimeOffset.Now:O}] {source}\n{ex}\n\n");
        }
        catch
        {
            // Logging must never itself throw during crash handling.
        }
    }

    public static void WriteLine(string message)
    {
        try
        {
            File.AppendAllText(Path, $"[{DateTimeOffset.Now:O}] {message}\n");
        }
        catch
        {
            // ignore
        }
    }
}
