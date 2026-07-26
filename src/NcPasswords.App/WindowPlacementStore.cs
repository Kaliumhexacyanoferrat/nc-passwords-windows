using System.IO;
using System.Text.Json;
using System.Windows;

namespace NcPasswords.App;

public sealed record WindowPlacementData(double Left, double Top, double Width, double Height, bool IsMaximized);

/// <summary>
/// Persists a window's size, position and maximized state across launches, keyed by a short name
/// (e.g. "Main", "Details"). Restoring is defensive about monitor setups changing between runs - a
/// saved rectangle that no longer overlaps the current virtual screen (a display was unplugged, the
/// resolution changed, ...) is discarded in favor of the window's normal XAML-defined defaults.
/// </summary>
public static class WindowPlacementStore
{
    private const double MinWidth = 320;
    private const double MinHeight = 240;
    private const double MinVisible = 80; // at least this much of the window must overlap a screen

    private static readonly string FilePath =
        Path.Combine(Core.Storage.AppPaths.DataDirectory, "window-placement.json");

    public static void Apply(Window window, string key)
    {
        if (!LoadAll().TryGetValue(key, out var data))
        {
            return;
        }

        var virtualScreen = VirtualScreen();
        var width = Math.Clamp(data.Width, MinWidth, Math.Max(MinWidth, virtualScreen.Width));
        var height = Math.Clamp(data.Height, MinHeight, Math.Max(MinHeight, virtualScreen.Height));
        var rect = new Rect(data.Left, data.Top, width, height);

        var overlap = Rect.Intersect(rect, virtualScreen);
        if (overlap.IsEmpty || overlap.Width < MinVisible || overlap.Height < MinVisible)
        {
            // The saved position no longer lands on any connected screen - keep the XAML defaults.
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = data.Left;
        window.Top = data.Top;
        window.Width = width;
        window.Height = height;

        if (data.IsMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    public static void Save(Window window, string key)
    {
        var isMaximized = window.WindowState == WindowState.Maximized;
        var bounds = isMaximized
            ? window.RestoreBounds
            : new Rect(window.Left, window.Top, window.Width, window.Height);

        if (bounds.Width <= 0 || bounds.Height <= 0 || double.IsNaN(bounds.Left) || double.IsNaN(bounds.Top))
        {
            return;
        }

        var all = LoadAll();
        all[key] = new WindowPlacementData(bounds.Left, bounds.Top, bounds.Width, bounds.Height, isMaximized);
        SaveAll(all);
    }

    private static Rect VirtualScreen() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    private static Dictionary<string, WindowPlacementData> LoadAll()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Dictionary<string, WindowPlacementData>>(json) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static void SaveAll(Dictionary<string, WindowPlacementData> all)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(all));
        }
        catch (Exception)
        {
            // Best-effort - losing the saved window placement isn't worth surfacing as an error.
        }
    }
}
