namespace AskMeNow.Core.Entities
{
   public class DocumentSnippet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string SnippetText { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public double RelevanceScore { get; set; }
        public List<string> HighlightedSentences { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;
    }
}
