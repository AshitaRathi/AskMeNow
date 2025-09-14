using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Application.Services;

public interface IFAQService
{
    Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath);
    Task<FAQAnswer> AnswerQuestionAsync(string question);
    Task<FAQAnswer> AnswerQuestionAsync(string question, string conversationId);
}

public class FAQService : IFAQService
{
    private readonly IDocumentCacheService _documentCacheService;
    private readonly IBedrockClientService _bedrockClientService;
    private readonly IDocumentChunkingService _chunkingService;
    private readonly IPersistentKnowledgeBaseService _knowledgeBaseService;
    private readonly IConversationService _conversationService;
    private readonly IAutoSuggestService _autoSuggestService;

    public FAQService(
        IDocumentCacheService documentCacheService, 
        IBedrockClientService bedrockClientService, 
        IDocumentChunkingService chunkingService,
        IPersistentKnowledgeBaseService knowledgeBaseService,
        IConversationService conversationService,
        IAutoSuggestService autoSuggestService)
    {
        _documentCacheService = documentCacheService;
        _bedrockClientService = bedrockClientService;
        _chunkingService = chunkingService;
        _knowledgeBaseService = knowledgeBaseService;
        _conversationService = conversationService;
        _autoSuggestService = autoSuggestService;
    }

    public async Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath)
    {
        // Process documents in the persistent knowledge base
        await _knowledgeBaseService.ProcessDocumentsInFolderAsync(folderPath);
        
        // Also load into cache for backward compatibility
        return await _documentCacheService.LoadAndCacheDocumentsAsync(folderPath);
    }

    public async Task<FAQAnswer> AnswerQuestionAsync(string question)
    {
        return await AnswerQuestionAsync(question, string.Empty);
    }

    public async Task<FAQAnswer> AnswerQuestionAsync(string question, string conversationId)
    {
        // Try to get relevant snippets from persistent knowledge base first
        var documentSnippets = await _knowledgeBaseService.FindRelevantSnippetsAsync(question, 5);
        
        string context;
        if (documentSnippets.Any())
        {
            // Use snippets from persistent knowledge base
            context = string.Join("\n\n", documentSnippets.Select(s => $"From {s.FileName}:\n{s.SnippetText}"));
        }
        else
        {
            // Fallback to cache-based approach
            context = _documentCacheService.GetRelevantContentForQuestion(question);
        }
        
        if (string.IsNullOrEmpty(context))
        {
            return new FAQAnswer
            {
                Question = question,
                Answer = "No documents have been loaded. Please select a folder with .txt files first.",
                Source = "System"
            };
        }

        // If we don't have snippets from knowledge base, get them from cache
        if (!documentSnippets.Any())
        {
            var documents = _documentCacheService.GetCachedDocuments();
            documentSnippets = _chunkingService.GetRelevantSnippets(question, documents, 5);
        }

        // Add conversation context if conversationId is provided
        string enhancedContext = context;
        if (!string.IsNullOrEmpty(conversationId))
        {
            var conversationContext = await _conversationService.GetConversationContextAsync(conversationId, 3);
            if (!string.IsNullOrEmpty(conversationContext))
            {
                enhancedContext = $"{conversationContext}\n\n{context}";
            }
        }

        var answer = await _bedrockClientService.GenerateAnswerAsync(question, enhancedContext);

        // Generate auto-suggestions
        var suggestedQuestions = await _autoSuggestService.GenerateSuggestionsAsync(question, answer, documentSnippets);

        var faqAnswer = new FAQAnswer
        {
            Question = question,
            Answer = answer,
            Source = "AI Assistant",
            SourceDocuments = new List<string> { "Knowledge Base" },
            DocumentSnippets = documentSnippets,
            SuggestedQuestions = suggestedQuestions
        };

        // Store the conversation if conversationId is provided
        if (!string.IsNullOrEmpty(conversationId))
        {
            await _conversationService.AddMessageAsync(conversationId, "User", question);
            await _conversationService.AddMessageAsync(conversationId, "AI", answer, question, answer);
        }

        return faqAnswer;
    }
}
