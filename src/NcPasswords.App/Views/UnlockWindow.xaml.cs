using System.Windows;
using System.Windows.Input;
using NcPasswords.App.ViewModels;

namespace NcPasswords.App.Views;

public partial class UnlockWindow : Window
{
    public UnlockWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private UnlockViewModel ViewModel => (UnlockViewModel)DataContext;

    private async void Unlock_Click(object sender, RoutedEventArgs e) => await SubmitAsync();

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SubmitAsync();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private async Task SubmitAsync()
    {
        if (await ViewModel.TryUnlockAsync(PasswordBox.Password))
        {
            DialogResult = true;
            Close();
        }
        else
        {
            PasswordBox.Clear();
            PasswordBox.Focus();
        }
    }
}
