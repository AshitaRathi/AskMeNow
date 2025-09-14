using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces;

public interface ISentimentAnalysisService
{
    Task<SentimentAnalysisResult> AnalyzeAsync(string text);
}
