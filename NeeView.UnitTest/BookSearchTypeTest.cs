using NeeLaboratory.IO.Search;
using System.Threading;
using Xunit;

namespace NeeView.UnitTest
{
    public class BookSearchTypeTest
    {
        [Theory]
        [InlineData("Folder", "folder")]
        [InlineData("CompressedFile", "compressedfile")]
        [InlineData("Media", "MEDIA")]
        [InlineData("Pdf", "pDf")]
        [InlineData("Playlist", "PLAYLIST")]
        public void TypeSearchMatchesArchiveFormatAndName(string format, string query)
        {
            var searcher = CreateSearcher();
            var matchingItem = CreateItem($"needle-{format}", format);
            var otherItem = CreateItem($"needle-other-{format}", format == "Folder" ? "CompressedFile" : "Folder");

            Assert.Single(searcher.Search($"/type {query} needle", new[] { matchingItem, otherItem }, default));
            Assert.Single(searcher.Search($"/type /eq {query} needle", new[] { matchingItem, otherItem }, default));
            Assert.Single(searcher.Search($"/type /ne {query} needle", new[] { matchingItem, otherItem }, default));
            Assert.Empty(searcher.Search($"/type /ne {query} needle", new[] { matchingItem }, default));
        }

        [Fact]
        public void TypeSearchRejectsUnknownFormat()
        {
            var context = CreateContext();
            var analyzer = new SearchKeyAnalyzer(context.KeyOptions, context.KeyAlias);
            Assert.Throws<SearchKeywordOptionException>(() => analyzer.Analyze("/type unknown"));
        }

        private static Searcher CreateSearcher() => new(CreateContext());

        private static SearchContext CreateContext() => new SearchContext().AddProfile(new BookSearchProfile());

        private static FileFolderItem CreateItem(string name, string format)
        {
            var type = format switch
            {
                "Folder" => ArchiveType.FolderArchive,
                "CompressedFile" => ArchiveType.ZipArchive,
                "Media" => ArchiveType.MediaArchive,
                "Pdf" => ArchiveType.PdfArchive,
                "Playlist" => ArchiveType.PlaylistArchive,
                _ => ArchiveType.None,
            };

            var extension = type switch
            {
                ArchiveType.FolderArchive => string.Empty,
                ArchiveType.ZipArchive => ".zip",
                ArchiveType.MediaArchive => ".mp4",
                ArchiveType.PdfArchive => ".pdf",
                ArchiveType.PlaylistArchive => ".nvpls",
                _ => ".txt",
            };
            var item = new FileFolderItem(false)
            {
                Name = name,
                TargetPath = new QueryPath($"{name}{extension}"),
            };
            if (type == ArchiveType.FolderArchive)
            {
                item.Attributes = FolderItemAttribute.Directory;
            }
            return item;
        }
    }
}
