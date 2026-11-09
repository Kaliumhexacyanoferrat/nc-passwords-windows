using System.Windows;
using NcPasswords.App.ViewModels;

namespace NcPasswords.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ServerUrlBox.Focus();

        // Stop waiting for the browser if the user gives up and closes the window.
        Closed += (_, _) => ViewModel.Cancel();
    }

    private LoginViewModel ViewModel => (LoginViewModel)DataContext;

    // The sign-in button is the window's default button, so Enter in any field lands here too.
    private async void SignIn_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.LoginAsync(UnlockPasswordBox.Password);
}
