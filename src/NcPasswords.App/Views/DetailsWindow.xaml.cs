using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    /// <summary>Toggles a secret field's visibility on a plain click, without interfering with click-drag text selection.</summary>
    private void SecretField_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox { DataContext: DetailField { IsSecret: true } field } textBox &&
            textBox.SelectionLength == 0)
        {
            field.IsRevealed = !field.IsRevealed;

            // The textbox's width changes with the toggle (mask vs. actual password length), which can
            // leave the mouse outside its new bounds without a move event. Layout is deferred, so force
            // it to run now before asking WPF to re-evaluate the cursor - otherwise it re-checks against
            // the stale (pre-toggle) bounds and the hand cursor stays stuck.
            textBox.UpdateLayout();
            Mouse.UpdateCursor();
        }
    }
}
