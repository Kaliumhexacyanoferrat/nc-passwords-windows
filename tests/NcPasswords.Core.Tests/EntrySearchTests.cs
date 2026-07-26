using NcPasswords.Core.Api;
using NcPasswords.Core.Organization;
using Xunit;

namespace NcPasswords.Core.Tests;

public class EntrySearchTests
{
    private static readonly List<PasswordEntry> Entries =
    [
        new() { Id = "1", Label = "GitHub", Username = "octocat", Url = "https://github.com", Notes = "" },
        new() { Id = "2", Label = "Email", Username = "bob@example.com", Url = "https://mail.example.com", Notes = "personal inbox" },
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Filter_ReturnsAllEntries_WhenQueryIsBlank(string? query)
    {
        var result = EntrySearch.Filter(Entries, query);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filter_MatchesLabelCaseInsensitively()
    {
        var result = EntrySearch.Filter(Entries, "github");
        Assert.Single(result);
        Assert.Equal("GitHub", result[0].Label);
    }

    [Fact]
    public void Filter_MatchesUsernameAndNotes()
    {
        Assert.Single(EntrySearch.Filter(Entries, "octocat"));
        Assert.Single(EntrySearch.Filter(Entries, "inbox"));
    }

    [Fact]
    public void Filter_ReturnsEmpty_WhenNothingMatches()
    {
        Assert.Empty(EntrySearch.Filter(Entries, "no-such-entry"));
    }
}
