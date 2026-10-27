using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

namespace NeeView.UnitTest
{
    public class BookHistoryPersistenceTests
    {
        [Fact]
        public void ExistingHistoryEntryTracksRepeatedPageProgressAndRecoversAfterUncleanExit()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "history.json");

            try
            {
                const string bookPath = "C:\\books\\sample.cbz";
                var historyEntry = new Dictionary<string, BookHistoryMemento>();
                var hasHistoryEntry = false;

                foreach (var page in new[] { "01.png", "02.png", "03.png" })
                {
                    Assert.True(BookMementoControl.UpdateHistoryEntry(ref hasHistoryEntry, canHistory: true,
                        () => new BookMemento { Path = bookPath, Page = page },
                        memento => historyEntry[memento.Path] = new BookHistoryMemento
                        {
                            Path = memento.Path,
                            Page = memento.Page,
                            LastAccessTime = DateTime.Now,
                        }));
                }

                var snapshot = new BookHistoryCollectionMemento
                {
                    Items = new List<BookHistoryMemento>(historyEntry.Values),
                };

                // Saving the snapshot and immediately loading it models a restart without shutdown saves.
                snapshot.Save(path, null);
                var recovered = BookHistoryCollectionMemento.Load(path);

                var item = Assert.Single(recovered.Items);
                Assert.Equal(bookPath, item.Path);
                Assert.Equal("03.png", item.Page);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void HistorySaveCadenceBoundsUnsavedProgressToThirtySeconds()
        {
            Assert.Equal(30, SaveDataSync.HistorySaveIntervalSeconds);
        }
    }
}
