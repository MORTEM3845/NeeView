using System.IO;
using System.Reflection;
using System.Windows;

namespace NeeView.UnitTest
{
    public class BookHistoryMoveTests
    {
        [Fact]
        public void History_marks_follow_files_back_to_their_original_folder()
        {
            if (Application.Current is null)
            {
                var app = new App();
                typeof(App).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, app);
            }

            var root = Path.Combine(Path.GetTempPath(), $"NeeViewHistoryMove-{Guid.NewGuid():N}");
            var source = Path.Combine(root, "source", "ReadSet");
            var destinationParent = Path.Combine(root, "destination");
            var moved = Path.Combine(destinationParent, "ReadSet");
            var sourcePage = Path.Combine(source, "page01.png");

            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destinationParent);
            File.WriteAllBytes(sourcePage, [1, 2, 3]);

            // Keep FileResolver's database isolated from the user's profile.
            Config.Current.Thumbnail.ThumbnailCacheFilePath = ":memory:";

            var history = BookHistoryCollection.Current;
            var mementos = BookMementoCollection.Current;
            history.Items.Add(new BookHistory(source, DateTime.Now));
            history.Items.Add(new BookHistory(sourcePage, DateTime.Now));
            mementos.Set(new BookMemento { Path = source });
            mementos.Set(new BookMemento { Path = sourcePage });

            try
            {
                // Moving through NeeView renames both the folder history and its
                // child history entries, recording their destination identities.
                Directory.Move(source, moved);
                BookMementoTools.RenameRecursive(source, moved);

                // Simulate moving the folder back outside NeeView. FileSystemWatcher
                // cannot pair this create with the earlier delete as a rename.
                Directory.Move(moved, source);

                Assert.True(history.Contains(source));
                Assert.True(history.Contains(sourcePage));
                Assert.False(history.Contains(Path.Combine(root, "unrelated.png")));
            }
            finally
            {
                history.Items.Clear();
                mementos.Clear();
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
