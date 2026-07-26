using NcPasswords.Core.Organization;

namespace NcPasswords.App.ViewModels;

/// <summary>Binding-friendly wrapper around a <see cref="FolderNode"/> for the tree view.</summary>
public sealed class FolderNodeViewModel
{
    public string FolderId { get; }
    public string Label { get; }
    public List<FolderNodeViewModel> Children { get; }
    public List<PasswordEntryViewModel> Passwords { get; }

    public FolderNodeViewModel(FolderNode node)
    {
        FolderId = node.Folder.Id;
        Label = string.IsNullOrWhiteSpace(node.Folder.Label) ? "(unnamed folder)" : node.Folder.Label;
        Children = node.Children.Select(c => new FolderNodeViewModel(c)).ToList();
        Passwords = node.Passwords.Select(p => new PasswordEntryViewModel(p)).ToList();
    }

    public static List<FolderNodeViewModel> BuildFrom(IReadOnlyList<FolderNode> roots) =>
        roots.Select(r => new FolderNodeViewModel(r)).ToList();
}
