namespace AskMeNow.Core.Entities
{
    public class DocumentInfo
    {
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public string Content { get; set; } = string.Empty;

        public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}