using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    public interface IConversationService
    {
        Task<string> CreateNewConversationAsync();
        Task<Conversation?> GetConversationAsync(string conversationId);
        Task<List<ChatMessage>> GetChatHistoryAsync(string conversationId, int maxTurns = 5);
        Task AddMessageAsync(string conversationId, string sender, string content, string? question = null, string? answer = null);
        Task UpdateConversationTitleAsync(string conversationId, string title);
        Task<List<Conversation>> GetAllConversationsAsync();
        Task DeleteConversationAsync(string conversationId);
        Task<string> GetConversationContextAsync(string conversationId, int maxTurns = 5);
    }
}
