using NcPasswords.Core.Api;

namespace NcPasswords.Core.Organization;

/// <summary>A folder plus its child folders and the passwords directly inside it, for tree-view display.</summary>
public sealed class FolderNode
{
    public required Folder Folder { get; init; }
    public List<FolderNode> Children { get; } = [];
    public List<PasswordEntry> Passwords { get; } = [];
}

/// <summary>Builds a folder tree (and attaches passwords to their folder) out of the flat lists the API returns.</summary>
public static class FolderTreeBuilder
{
    public static IReadOnlyList<FolderNode> Build(
        IReadOnlyList<Folder> folders,
        IReadOnlyList<PasswordEntry> passwords)
    {
        var nodesById = folders.ToDictionary(f => f.Id, f => new FolderNode { Folder = f });
        var roots = new List<FolderNode>();

        foreach (var folder in folders)
        {
            var node = nodesById[folder.Id];
            if (folder.Parent != Folder.RootId && nodesById.TryGetValue(folder.Parent, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        foreach (var password in passwords)
        {
            if (nodesById.TryGetValue(password.Folder, out var owner))
            {
                owner.Passwords.Add(password);
            }
            else
            {
                // Entry references a folder we don't have (e.g. root, or a trashed/missing folder)
                // - surface it at the root so nothing silently disappears from the tree view.
                roots.Add(new FolderNode { Folder = new Folder { Id = password.Folder, Label = "" } });
                nodesById[password.Folder] = roots[^1];
                roots[^1].Passwords.Add(password);
            }
        }

        return roots;
    }
}
