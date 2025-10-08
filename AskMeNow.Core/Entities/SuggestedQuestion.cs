namespace AskMeNow.Core.Entities
{
    public class SuggestedQuestion
    {
        public string Question { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
