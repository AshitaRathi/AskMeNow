using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AskMeNow.Infrastructure.Services
{
    public class ConversationService : IConversationService
    {
        private readonly KnowledgeBaseContext _context;

        public ConversationService(KnowledgeBaseContext context)
        {
            _context = context;
        }

        public async Task<string> CreateNewConversationAsync()
        {
            var conversation = new Conversation
            {
                ConversationId = Guid.NewGuid().ToString(),
                Title = "New Conversation",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation.ConversationId;
        }

        public async Task<Conversation?> GetConversationAsync(string conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string conversationId, int maxTurns = 5)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == _context.Conversations
                    .Where(c => c.ConversationId == conversationId)
                    .Select(c => c.Id)
                    .FirstOrDefault())
                .OrderByDescending(m => m.TurnNumber)
                .Take(maxTurns * 2)
                .OrderBy(m => m.TurnNumber)
                .ToListAsync();
        }

        public async Task AddMessageAsync(string conversationId, string sender, string content, string? question = null, string? answer = null)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null)
                return;

            var lastTurnNumber = await _context.ChatMessages
                .Where(m => m.ConversationId == conversation.Id)
                .MaxAsync(m => (int?)m.TurnNumber) ?? 0;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                Sender = sender,
                Content = content,
                Question = question,
                Answer = answer,
                TurnNumber = lastTurnNumber + 1,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);

            conversation.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateConversationTitleAsync(string conversationId, string title)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation != null)
            {
                conversation.Title = title;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Conversation>> GetAllConversationsAsync()
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .OrderByDescending(c => c.LastActivityAt)
                .ToListAsync();
        }

        public async Task DeleteConversationAsync(string conversationId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation != null)
            {
                _context.Conversations.Remove(conversation);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetConversationContextAsync(string conversationId, int maxTurns = 5)
        {
            var messages = await GetChatHistoryAsync(conversationId, maxTurns);

            if (!messages.Any())
                return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("Previous conversation context:");

            foreach (var message in messages)
            {
                if (message.Sender == "User")
                {
                    context.AppendLine($"User: {message.Content}");
                }
                else if (message.Sender == "AI")
                {
                    context.AppendLine($"AI: {message.Content}");
                }
            }

            context.AppendLine("\nCurrent question:");
            return context.ToString();
        }
    }
}