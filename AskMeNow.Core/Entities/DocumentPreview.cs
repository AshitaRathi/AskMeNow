namespace AskMeNow.Core.Entities
{
    public class DocumentPreview
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public int WordCount { get; set; }
        public long FileSizeBytes { get; set; }
        public List<DocumentHighlight> Highlights { get; set; } = new();
        public List<DocumentSnippet> ReferencedSnippets { get; set; } = new();
    }

    public class DocumentHighlight
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public HighlightType Type { get; set; }
        public double RelevanceScore { get; set; }
        public int ReferenceCount { get; set; }
        public DateTime LastReferenced { get; set; }
        public string Tooltip { get; set; } = string.Empty;
    }

    public enum HighlightType
    {
        FrequentlyReferenced,
        HighRelevance,
        RecentReference,
        KeyConcept
    }

}