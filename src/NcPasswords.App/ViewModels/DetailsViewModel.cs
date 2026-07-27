using CommunityToolkit.Mvvm.ComponentModel;
using NcPasswords.Core.Api;

namespace NcPasswords.App.ViewModels;

/// <summary>A single labeled detail value. Secret fields (the password) display as asterisks until clicked.</summary>
public sealed partial class DetailField : ObservableObject
{
    private const string Mask = "**********";

    public string Label { get; }
    public string Value { get; }
    public bool IsSecret { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private bool _isRevealed;

    public DetailField(string label, string value, bool isSecret = false)
    {
        Label = label;
        Value = value;
        IsSecret = isSecret;
    }

    public string DisplayValue => IsSecret && !IsRevealed ? Mask : Value;
}

/// <summary>Flattens a <see cref="PasswordEntry"/> (including its custom fields) for read-only detail display.</summary>
public sealed class DetailsViewModel
{
    public string Title { get; }
    public bool Favorite { get; }
    public IReadOnlyList<DetailField> Fields { get; }
    public IReadOnlyList<CustomField> CustomFields { get; }
    public IReadOnlyList<string> Tags { get; }

    public DetailsViewModel(PasswordEntry entry)
    {
        Title = entry.Label;
        Favorite = entry.Favorite;
        Tags = entry.Tags.Select(t => t.Label).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        CustomFields = CustomFieldParser.Parse(entry.CustomFields);

        Fields =
        [
            new DetailField("Username", entry.Username),
            new DetailField("Password", entry.Password, isSecret: true),
            new DetailField("URL", entry.Url),
            new DetailField("Notes", entry.Notes),
            new DetailField("Created", entry.CreatedAt.LocalDateTime.ToString("g")),
            new DetailField("Updated", entry.UpdatedAt.LocalDateTime.ToString("g")),
        ];
    }
}
