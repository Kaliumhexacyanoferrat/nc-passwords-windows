using NcPasswords.Core.Api;

namespace NcPasswords.App.ViewModels;

public sealed record DetailField(string Label, string Value);

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
            new DetailField("Password", entry.Password),
            new DetailField("URL", entry.Url),
            new DetailField("Notes", entry.Notes),
            new DetailField("Created", entry.CreatedAt.LocalDateTime.ToString("g")),
            new DetailField("Updated", entry.UpdatedAt.LocalDateTime.ToString("g")),
        ];
    }
}
