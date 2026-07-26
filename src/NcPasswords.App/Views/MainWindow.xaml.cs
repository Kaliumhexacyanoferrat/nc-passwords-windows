using System.Windows;
using System.Windows.Input;
using NcPasswords.App.ViewModels;

namespace NcPasswords.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
        ViewModel.SelectFolder(e.NewValue as FolderNodeViewModel);

    private void EntriesList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control &&
            EntriesList.SelectedItem is PasswordEntryViewModel entry)
        {
            ViewModel.CopyPassword(entry);
            e.Handled = true;
        }
    }

    private void CopyUsernameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PasswordEntryViewModel entry)
        {
            ViewModel.CopyUsername(entry);
        }
    }

    private void CopyPasswordMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PasswordEntryViewModel entry)
        {
            ViewModel.CopyPassword(entry);
        }
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PasswordEntryViewModel entry)
        {
            var window = new DetailsWindow(entry.Entry) { Owner = this };
            window.ShowDialog();
        }
    }
}
