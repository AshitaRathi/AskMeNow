using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces;

public interface ISmallTalkService
{
    Task<string> GetResponseAsync(string userMessage, SentimentAnalysisResult analysis);
    bool CanHandle(SentimentAnalysisResult analysis);
}
