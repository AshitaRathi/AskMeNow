namespace AskMeNow.Core.Entities;

public enum MessageSender
{
    User,
    AI
}

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public MessageSender Sender { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsLoading { get; set; } = false;
    public List<DocumentSnippet> DocumentSnippets { get; set; } = new();
    public List<SuggestedQuestion> SuggestedQuestions { get; set; } = new();
}
