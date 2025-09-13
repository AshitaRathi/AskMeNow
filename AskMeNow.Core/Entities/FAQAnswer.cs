namespace AskMeNow.Core.Entities;

public class FAQAnswer
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public List<string> SourceDocuments { get; set; } = new();
}
