using NcPasswords.Core.Organization;

namespace NcPasswords.App.ViewModels;

/// <summary>
/// Binding-friendly wrapper around a <see cref="FolderNode"/> for a combined folder/entry tree view
/// (à la Password Safe): each node's <see cref="Items"/> mixes child folders and the passwords stored
/// directly in it, sorted together alphabetically.
/// </summary>
public sealed class FolderNodeViewModel
{
    public string FolderId { get; }
    public string Label { get; }
    public List<object> Items { get; }

    private FolderNodeViewModel(FolderNode node, bool pruneEmptyFolders)
    {
        FolderId = node.Folder.Id;
        Label = string.IsNullOrWhiteSpace(node.Folder.Label) ? "(unnamed folder)" : node.Folder.Label;

        var children = node.Children
            .Select(c => new FolderNodeViewModel(c, pruneEmptyFolders))
            .Where(c => !pruneEmptyFolders || c.Items.Count > 0);
        var passwords = node.Passwords.Select(p => new PasswordEntryViewModel(p));

        Items = Merge(children, passwords);
    }

    /// <summary>
    /// Builds the root-level tree items. Root-level passwords (no folder, or referencing a folder we
    /// don't have) are surfaced directly instead of nested under the blank-label placeholder folder
    /// <see cref="FolderTreeBuilder"/> wraps them in.
    /// </summary>
    public static List<object> BuildRoot(IReadOnlyList<FolderNode> roots, bool pruneEmptyFolders)
    {
        var folders = new List<FolderNodeViewModel>();
        var rootPasswords = new List<PasswordEntryViewModel>();

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root.Folder.Label) && root.Children.Count == 0)
            {
                rootPasswords.AddRange(root.Passwords.Select(p => new PasswordEntryViewModel(p)));
            }
            else
            {
                var vm = new FolderNodeViewModel(root, pruneEmptyFolders);
                if (!pruneEmptyFolders || vm.Items.Count > 0)
                {
                    folders.Add(vm);
                }
            }
        }

        return Merge(folders, rootPasswords);
    }

    private static List<object> Merge(IEnumerable<FolderNodeViewModel> folders, IEnumerable<PasswordEntryViewModel> passwords) =>
        folders.Cast<object>()
            .Concat(passwords)
            .OrderBy(SortLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string SortLabel(object item) => item switch
    {
        FolderNodeViewModel folder => folder.Label,
        PasswordEntryViewModel password => password.Label,
        _ => "",
    };
}
