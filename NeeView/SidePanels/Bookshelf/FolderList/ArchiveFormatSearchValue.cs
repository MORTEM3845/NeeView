using NeeLaboratory.IO.Search;
using System;

namespace NeeView
{
    /// <summary>
    /// Search value for the user-facing archive format names.
    /// </summary>
    public class ArchiveFormatSearchValue : SearchValue
    {
        private readonly string _value;

        public ArchiveFormatSearchValue() : this(string.Empty)
        {
        }

        public ArchiveFormatSearchValue(string value)
        {
            _value = value;
        }

        public override Type GetValueType() => typeof(ArchiveFormatSearchValue);

        public override int CompareTo(SearchValue other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(_value, ((ArchiveFormatSearchValue)other)._value);
        }

        public override SearchValue Parse(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "folder" => new ArchiveFormatSearchValue("Folder"),
                "compressedfile" => new ArchiveFormatSearchValue("CompressedFile"),
                "media" => new ArchiveFormatSearchValue("Media"),
                "pdf" => new ArchiveFormatSearchValue("Pdf"),
                "playlist" => new ArchiveFormatSearchValue("Playlist"),
                _ => throw new SearchKeywordOptionException($"Unknown archive format: {value}"),
            };
        }

        public override string ToString() => _value;

        public static string? GetName(ArchiveType archiveType) => archiveType switch
        {
            ArchiveType.FolderArchive => "Folder",
            ArchiveType.ZipArchive or ArchiveType.SevenZipArchive or ArchiveType.SusieArchive => "CompressedFile",
            ArchiveType.MediaArchive => "Media",
            ArchiveType.PdfArchive => "Pdf",
            ArchiveType.PlaylistArchive => "Playlist",
            _ => null,
        };

        public static string? GetName(FolderItem item)
        {
            if (item.IsDirectory)
            {
                return "Folder";
            }

            return GetName(ArchiveManager.Current.GetSupportedType(item.TargetPath.SimplePath));
        }
    }
}
