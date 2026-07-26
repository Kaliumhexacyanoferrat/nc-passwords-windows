using NcPasswords.Core.Api;

namespace NcPasswords.App.ViewModels;

/// <summary>Thin, read-only display wrapper around a <see cref="PasswordEntry"/> for list/tree binding.</summary>
public sealed class PasswordEntryViewModel(PasswordEntry entry)
{
    public PasswordEntry Entry { get; } = entry;

    public string Id => Entry.Id;
    public string Label => Entry.Label;
    public string Username => Entry.Username;
    public string Password => Entry.Password;
    public string Url => Entry.Url;
    public bool Favorite => Entry.Favorite;
    public DateTimeOffset Updated => Entry.UpdatedAt;

    /// <summary>"Label [Username]" for the combined folder/entry tree - falls back to just the label when there's no username.</summary>
    public string DisplayLabel => string.IsNullOrEmpty(Username) ? Label : $"{Label} [{Username}]";
}
