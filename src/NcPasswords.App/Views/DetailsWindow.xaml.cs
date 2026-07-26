using System.Windows;
using NcPasswords.App.ViewModels;
using NcPasswords.Core.Api;

namespace NcPasswords.App.Views;

public partial class DetailsWindow : Window
{
    private const string PlacementKey = "Details";

    public DetailsWindow(PasswordEntry entry)
    {
        InitializeComponent();
        DataContext = new DetailsViewModel(entry);
        WindowPlacementStore.Apply(this, PlacementKey);
        Closing += (_, _) => WindowPlacementStore.Save(this, PlacementKey);
    }

    private void CopyField_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is string value)
        {
            try
            {
                Clipboard.SetText(value);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Clipboard busy; ignore.
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
