using NcPasswords.Core.Api;

namespace NcPasswords.Core.Organization;

public static class EntrySearch
{
    public static IReadOnlyList<PasswordEntry> Filter(IReadOnlyList<PasswordEntry> entries, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return entries;
        }

        return entries.Where(e => Matches(e, query)).ToList();
    }

    public static bool Matches(PasswordEntry entry, string query) =>
        Contains(entry.Label, query) ||
        Contains(entry.Username, query) ||
        Contains(entry.Url, query) ||
        Contains(entry.Notes, query);

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
