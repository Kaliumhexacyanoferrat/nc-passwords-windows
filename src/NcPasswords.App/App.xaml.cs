using System.Windows;
using System.Windows.Threading;
using NcPasswords.App.ViewModels;
using NcPasswords.App.Views;
using NcPasswords.Core.Storage;

namespace NcPasswords.App;

public partial class App : Application
{
    // Tracked explicitly rather than read back from Application.Current.MainWindow: WPF
    // auto-assigns MainWindow to the first Window ever constructed in the process, so on the
    // "already signed in" startup path (MainWindow is that first window) reading it back here
    // would return the window we just showed, and closing "the previous window" would close it too.
    private static Window? _currentWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            var credentialStore = new CredentialStore();
            var stored = credentialStore.Load();

            if (stored is null)
            {
                ShowLogin();
            }
            else
            {
                ShowMain(stored);
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("OnStartup", ex);
            ShowFatalError(ex);
            Shutdown(-1);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        CrashLog.Write("DispatcherUnhandledException", args.Exception);
        ShowFatalError(args.Exception);
        args.Handled = true;
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception ex)
        {
            CrashLog.Write("AppDomain.UnhandledException", ex);
            ShowFatalError(ex);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        CrashLog.Write("TaskScheduler.UnobservedTaskException", args.Exception);
        args.SetObserved();
    }

    private static void ShowFatalError(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex}",
                "NcPasswords - Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // If we can't even show a dialog, the crash.log entry is the only record left.
        }
    }

    public static void ShowLogin()
    {
        var loginViewModel = new LoginViewModel(onLoggedIn: ShowMain);
        var loginWindow = new LoginWindow { DataContext = loginViewModel };
        SwapWindow(loginWindow);
    }

    public static void ShowMain(StoredCredentials credentials)
    {
        var mainViewModel = new MainViewModel(credentials, onSignedOut: ShowLogin);
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        SwapWindow(mainWindow);

        _ = mainViewModel.InitializeAsync().ContinueWith(
            t => CrashLog.Write("InitializeAsync", t.Exception!),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private static void SwapWindow(Window next)
    {
        var previous = _currentWindow;
        _currentWindow = next;
        Current.MainWindow = next;
        next.Show();
        previous?.Close();
    }
}
