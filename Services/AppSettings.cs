using System.ComponentModel.DataAnnotations;

namespace PDFArchiveViewer.Services
{
    public class AppSettings
    {
        [Required(ErrorMessage = "ArchiveRootFolder is missing or empty in appsettings.json")]
        public string ArchiveRootFolder { get; set; } = "";
        public string Format { get; set; } = "";

        public string SiteTitle { get; set; } = "Archive Viewer";
        public string SiteImage { get; set; } = "";

        // Case-insensitive so "np1" on disk still matches "NP1" in the config
        public Dictionary<string, string> FolderDisplayNames { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        // "//server/share/folder" -> "\\server\share\folder"
        public string GetRootPath() =>
            ArchiveRootFolder.Replace('/', '\\').TrimEnd('\\');

        // Falls back to the real folder name if no display name is set
        public string GetDisplayName(string folderName) =>
            FolderDisplayNames.TryGetValue(folderName, out var name) ? name : folderName;
    }
}
