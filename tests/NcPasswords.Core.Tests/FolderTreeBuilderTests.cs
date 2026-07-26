using NcPasswords.Core.Api;
using NcPasswords.Core.Organization;
using Xunit;

namespace NcPasswords.Core.Tests;

public class FolderTreeBuilderTests
{
    [Fact]
    public void Build_NestsChildFoldersUnderTheirParent()
    {
        var root = new Folder { Id = "root", Label = "Root", Parent = Folder.RootId };
        var child = new Folder { Id = "child", Label = "Child", Parent = "root" };
        var folders = new List<Folder> { root, child };

        var tree = FolderTreeBuilder.Build(folders, []);

        var rootNode = Assert.Single(tree);
        Assert.Equal("Root", rootNode.Folder.Label);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal("Child", childNode.Folder.Label);
    }

    [Fact]
    public void Build_AttachesPasswordsToTheirOwningFolder()
    {
        var folder = new Folder { Id = "f1", Label = "Personal", Parent = Folder.RootId };
        var entry = new PasswordEntry { Id = "p1", Label = "Bank", Folder = "f1" };

        var tree = FolderTreeBuilder.Build([folder], [entry]);

        var node = Assert.Single(tree);
        var password = Assert.Single(node.Passwords);
        Assert.Equal("Bank", password.Label);
    }

    [Fact]
    public void Build_SurfacesOrphanedPasswords_InsteadOfDroppingThem()
    {
        var entry = new PasswordEntry { Id = "p1", Label = "Orphan", Folder = "missing-folder" };

        var tree = FolderTreeBuilder.Build([], [entry]);

        var node = Assert.Single(tree);
        Assert.Equal("Orphan", Assert.Single(node.Passwords).Label);
    }
}
