using Xunit;

namespace NeeView.UnitTest;

public class FolderListBoundaryTests
{
    private static FolderItem CreateItem(string path)
    {
        var item = new FileFolderItem(false);
        item.SetTargetPathAndName(new QueryPath(path), path);
        return item;
    }

    private static FolderItem CreateSystemItem(string path)
    {
        var item = CreateItem(path);
        item.Attributes |= FolderItemAttribute.System;
        return item;
    }

    [Fact]
    public void BoundaryFolder_IsNullForEmptyItems()
    {
        Assert.Null(FolderList.GetBoundaryFolder([], false));
        Assert.Null(FolderList.GetBoundaryFolder([], true));
    }

    [Fact]
    public void BoundaryFolder_ReturnsOnlyItemForSingleItem()
    {
        var only = CreateItem("only.txt");

        Assert.Same(only, FolderList.GetBoundaryFolder([only], false));
        Assert.Same(only, FolderList.GetBoundaryFolder([only], true));
    }

    [Fact]
    public void BoundaryFolder_ReturnsFirstOrLastRegardlessOfCurrentSelection()
    {
        var first = CreateItem("first.txt");
        var middle = CreateItem("middle.txt");
        var last = CreateItem("last.txt");
        Assert.Same(first, FolderList.GetBoundaryFolder([first, middle, last], false));
        Assert.Same(last, FolderList.GetBoundaryFolder([first, middle, last], true));
        Assert.Same(first, FolderList.GetBoundaryFolder([first, middle, last], false));
        Assert.Same(last, FolderList.GetBoundaryFolder([first, middle, last], true));
    }

    [Fact]
    public void BoundaryFolder_SkipsSystemItems()
    {
        var system = CreateSystemItem("parent");
        var first = CreateItem("first.txt");
        var last = CreateItem("last.txt");
        var systemLast = CreateSystemItem("empty");

        Assert.Same(first, FolderList.GetBoundaryFolder([system, first, last, systemLast], false));
        Assert.Same(last, FolderList.GetBoundaryFolder([system, first, last, systemLast], true));
    }
}
