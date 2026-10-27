using NeeView.Collections.Generic;

namespace NeeView.UnitTest
{
    public sealed class BookmarkCollectionRenameTests : IDisposable
    {
        private const string _sourcePath = @"C:\Books\sample.zip";
        private const string _childPath = @"C:\Books\Child\sample.zip";

        [Fact]
        public void MoveToChildPathRemovesDestinationDuplicateAndPreservesBookmarkIdentity()
        {
            var root = CreateRoot(
                new Bookmark(_sourcePath),
                new Bookmark(_childPath));
            var sourceNode = root[0];
            var duplicateNode = root[1];
            var sourceBookmark = sourceNode.Value;
            Load(root);

            var changes = new List<BookmarkCollectionChangedEventArgs>();
            EventHandler<BookmarkCollectionChangedEventArgs> handler = (_, e) => changes.Add(e);
            BookmarkCollection.Current.BookmarkChanged += handler;
            try
            {
                BookmarkCollection.Current.Rename(_sourcePath, _childPath);
            }
            finally
            {
                BookmarkCollection.Current.BookmarkChanged -= handler;
            }

            Assert.Single(root.Children);
            Assert.Same(sourceNode, root[0]);
            Assert.Same(sourceBookmark, sourceNode.Value);
            Assert.Equal(_childPath, ((Bookmark)sourceNode.Value).Path);
            Assert.Contains(changes, e => e.Action == EntryCollectionChangedAction.Remove && e.Item == duplicateNode);
            Assert.Contains(changes, e => e.Action == EntryCollectionChangedAction.Rename && e.Item == sourceNode);
            Assert.Null(BookmarkCollectionService.AddTo(new QueryPath(_childPath), root, null, false));
        }

        [Fact]
        public void RenameBookmarkPathPreservesBookmarkNode()
        {
            const string renamedPath = @"C:\Books\renamed.zip";
            var root = CreateRoot(new Bookmark(_sourcePath));
            var bookmarkNode = root[0];
            Load(root);

            BookmarkCollection.Current.Rename(_sourcePath, renamedPath);

            Assert.Same(bookmarkNode, root[0]);
            Assert.Equal(renamedPath, ((Bookmark)bookmarkNode.Value).Path);
            Assert.Null(BookmarkCollection.Current.Find(_sourcePath));
            Assert.Same(bookmarkNode.Value, BookmarkCollection.Current.Find(renamedPath));
        }

        [Fact]
        public void RenameToSamePathDoesNothing()
        {
            var root = CreateRoot(new Bookmark(_sourcePath));
            var bookmarkNode = root[0];
            Load(root);
            var changeCount = 0;
            EventHandler<BookmarkCollectionChangedEventArgs> handler = (_, _) => changeCount++;
            BookmarkCollection.Current.BookmarkChanged += handler;
            try
            {
                BookmarkCollection.Current.Rename(_sourcePath, _sourcePath);
            }
            finally
            {
                BookmarkCollection.Current.BookmarkChanged -= handler;
            }

            Assert.Equal(0, changeCount);
            Assert.Same(bookmarkNode, root[0]);
            Assert.Equal(_sourcePath, ((Bookmark)bookmarkNode.Value).Path);
        }

        [Fact]
        public void RenameKeepsSamePathBookmarksInDifferentFolders()
        {
            var root = new TreeListNode<IBookmarkEntry>(new BookmarkFolder());
            var firstFolder = new TreeListNode<IBookmarkEntry>(new BookmarkFolder("First", null, DateTime.Now));
            var secondFolder = new TreeListNode<IBookmarkEntry>(new BookmarkFolder("Second", null, DateTime.Now));
            root.Add(firstFolder);
            root.Add(secondFolder);
            firstFolder.Add(new TreeListNode<IBookmarkEntry>(new Bookmark(_sourcePath)));
            secondFolder.Add(new TreeListNode<IBookmarkEntry>(new Bookmark(_childPath)));
            Load(root);

            BookmarkCollection.Current.Rename(_sourcePath, _childPath);

            Assert.Equal(2, BookmarkCollection.Current.Collect(_childPath).Count);
            Assert.Single(firstFolder.Children);
            Assert.Single(secondFolder.Children);
        }

        private static TreeListNode<IBookmarkEntry> CreateRoot(params Bookmark[] bookmarks)
        {
            var root = new TreeListNode<IBookmarkEntry>(new BookmarkFolder());
            foreach (var bookmark in bookmarks)
            {
                root.Add(new TreeListNode<IBookmarkEntry>(bookmark));
            }
            return root;
        }

        private static void Load(TreeListNode<IBookmarkEntry> root)
        {
            BookMementoCollection.Current.Clear();
            BookmarkCollection.Current.Load(root, Array.Empty<BookMemento>());
        }

        public void Dispose()
        {
            BookMementoCollection.Current.Clear();
            BookmarkCollection.Current.Load(new TreeListNode<IBookmarkEntry>(new BookmarkFolder()), Array.Empty<BookMemento>());
        }
    }
}
