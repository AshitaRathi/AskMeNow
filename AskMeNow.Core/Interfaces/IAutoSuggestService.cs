using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    public interface IAutoSuggestService
    {
        Task<List<SuggestedQuestion>> GenerateSuggestionsAsync(string question, string answer, List<DocumentSnippet>? documentSnippets = null);
        Task<List<SuggestedQuestion>> GenerateContextualSuggestionsAsync(string conversationId, string currentAnswer);
    }
}
