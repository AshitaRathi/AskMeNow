namespace AskMeNow.Core.Entities;

public class FAQAnswer
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public List<string> SourceDocuments { get; set; } = new();
    public List<DocumentSnippet> DocumentSnippets { get; set; } = new();
    public List<SuggestedQuestion> SuggestedQuestions { get; set; } = new();
}
