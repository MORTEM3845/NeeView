using NeeLaboratory.IO.Search;

namespace NeeView
{
    public class BookSearchProfile : SearchProfile
    {
        public BookSearchProfile()
        {
            Options.Add(BookSearchPropertyProfiles.IsBookmark);
            Options.Add(BookSearchPropertyProfiles.IsHistory);
            Options.Add(BookSearchPropertyProfiles.Type);

            Alias.Add("/bookmark", new() { "/p.bookmark", "/m.eq", "true" });
            Alias.Add("/history", new() { "/p.history", "/m.eq", "true" });
            Alias.Add("/type", new() { "/p.type", "/m.eq" });
        }
    }

    public static class BookSearchPropertyProfiles
    {
        public static SearchPropertyProfile IsBookmark { get; } = new SearchPropertyProfile("bookmark", BooleanSearchValue.Default);
        public static SearchPropertyProfile IsHistory { get; } = new SearchPropertyProfile("history", BooleanSearchValue.Default);
        public static SearchPropertyProfile Type { get; } = new SearchPropertyProfile("type", new ArchiveFormatSearchValue());
    }


    public class FileSearchProfile : SearchProfile
    {
        public FileSearchProfile()
        {
            Options.Add(FileSearchPropertyProfiles.IsDirectory);

            Alias.Add("/directory", new() { "/p.directory", "/m.eq" });
        }
    }

    public static class FileSearchPropertyProfiles
    {
        public static SearchPropertyProfile IsDirectory { get; } = new SearchPropertyProfile("directory", BooleanSearchValue.Default);
    }

}
