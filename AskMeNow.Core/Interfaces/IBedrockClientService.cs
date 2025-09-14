namespace AskMeNow.Core.Interfaces;

public interface IBedrockClientService
{
    Task<string> GenerateAnswerAsync(string question, string context);
    Task<string> GenerateSuggestionsAsync(string prompt);
}
