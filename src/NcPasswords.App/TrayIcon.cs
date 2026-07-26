using System.ComponentModel;
using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace NcPasswords.App;

/// <summary>
/// Owns the system tray icon for the app's lifetime. Windows attached via <see cref="AttachTo"/>
/// hide instead of closing when the user clicks their close button; the tray icon's "Open" entry
/// (or double/single-click) brings the most recently attached window back.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private Window? _window;
    private bool _isExiting;

    public TrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "NcPasswords",
            Visible = true,
        };
        _notifyIcon.MouseClick += OnTrayMouseClick;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => RestoreWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Exit());
        _notifyIcon.ContextMenuStrip = menu;
    }

    public void AttachTo(Window window)
    {
        _window = window;
        window.Closing += OnWindowClosing;
    }

    public void Detach(Window window)
    {
        window.Closing -= OnWindowClosing;
        if (ReferenceEquals(_window, window))
        {
            _window = null;
        }
    }

    private void OnTrayMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            RestoreWindow();
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        ((Window)sender!).Hide();
    }

    private void RestoreWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    public void Exit()
    {
        _isExiting = true;
        _notifyIcon.Visible = false;
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static Drawing.Icon LoadIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico");
        var resourceStream = Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException("Tray icon resource 'Assets/app.ico' was not found.");

        using var stream = resourceStream.Stream;
        return new Drawing.Icon(stream);
    }
}
