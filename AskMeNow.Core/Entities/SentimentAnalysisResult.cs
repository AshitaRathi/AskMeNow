namespace AskMeNow.Core.Entities
{
    public enum Sentiment
    {
        Positive,
        Negative,
        Neutral
    }

    public enum Intent
    {
        Greeting,
        SmallTalk,
        Question,
        Complaint,
        Other
    }

    public class SentimentAnalysisResult
    {
        public Sentiment Sentiment { get; set; }
        public Intent Intent { get; set; }
        public double Confidence { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }

}