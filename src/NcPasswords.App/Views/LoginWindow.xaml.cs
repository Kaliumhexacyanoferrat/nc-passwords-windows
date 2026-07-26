using System.Windows;
using System.Windows.Input;
using NcPasswords.App.ViewModels;

namespace NcPasswords.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private LoginViewModel ViewModel => (LoginViewModel)DataContext;

    private async void SignIn_Click(object sender, RoutedEventArgs e) => await SubmitAsync();

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SubmitAsync();
        }
    }

    private async Task SubmitAsync() => await ViewModel.LoginAsync(PasswordBox.Password);
}
