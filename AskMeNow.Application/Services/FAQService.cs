using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Application.Services
{
    public class FAQService : IFAQService
    {
        private readonly IDocumentCacheService _documentCacheService;
        private readonly IBedrockClientService _bedrockClientService;
        private readonly IDocumentChunkingService _chunkingService;
        private readonly IPersistentKnowledgeBaseService _knowledgeBaseService;
        private readonly IConversationService _conversationService;
        private readonly IAutoSuggestService _autoSuggestService;
        private readonly IEnhancedRetrievalService _enhancedRetrievalService;
        private readonly IEnhancedContextService _enhancedContextService;

        public FAQService(
            IDocumentCacheService documentCacheService,
            IBedrockClientService bedrockClientService,
            IDocumentChunkingService chunkingService,
            IPersistentKnowledgeBaseService knowledgeBaseService,
            IConversationService conversationService,
            IAutoSuggestService autoSuggestService,
            IEnhancedRetrievalService enhancedRetrievalService,
            IEnhancedContextService enhancedContextService)
        {
            _documentCacheService = documentCacheService;
            _bedrockClientService = bedrockClientService;
            _chunkingService = chunkingService;
            _knowledgeBaseService = knowledgeBaseService;
            _conversationService = conversationService;
            _autoSuggestService = autoSuggestService;
            _enhancedRetrievalService = enhancedRetrievalService;
            _enhancedContextService = enhancedContextService;
        }

        public async Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath)
        {
            await _knowledgeBaseService.ProcessDocumentsInFolderAsync(folderPath);

            return await _documentCacheService.LoadAndCacheDocumentsAsync(folderPath);
        }

        public async Task<FAQAnswer> AnswerQuestionAsync(string question)
        {
            return await AnswerQuestionAsync(question, string.Empty);
        }

        public async Task<FAQAnswer> AnswerQuestionAsync(string question, string conversationId)
        {
            var retrievalResults = await _enhancedRetrievalService.RetrieveRelevantChunksAsync(question, 10, 0.1f);

            string? conversationContext = null;
            if (!string.IsNullOrEmpty(conversationId))
            {
                conversationContext = await _conversationService.GetConversationContextAsync(conversationId, 3);
            }

            bool hasSufficientContext = _enhancedContextService.HasSufficientContext(retrievalResults, 0.3f);

            List<FallbackSuggestion>? fallbackSuggestions = null;
            if (!hasSufficientContext)
            {
                fallbackSuggestions = await _enhancedRetrievalService.GetFallbackSuggestionsAsync(question, 5);
            }

            var enhancedContext = await _enhancedContextService.BuildContextAsync(
                question,
                retrievalResults,
                conversationContext,
                fallbackSuggestions);

            string answer;
            List<DocumentSnippet> documentSnippets = new();

            if (hasSufficientContext)
            {
                answer = await _bedrockClientService.GenerateAnswerAsync(question, enhancedContext);

                documentSnippets = retrievalResults.Select(r => new DocumentSnippet
                {
                    FileName = r.Chunk.SourceDocument,
                    SnippetText = r.Chunk.Content,
                    RelevanceScore = r.SimilarityScore,
                    FilePath = r.Chunk.FilePath,
                    StartIndex = 0,
                    EndIndex = r.Chunk.Content.Length
                }).ToList();
            }
            else
            {
                answer = _enhancedContextService.FormatFallbackResponse(question, fallbackSuggestions ?? new List<FallbackSuggestion>(), retrievalResults.Any());
            }

            // Generate auto-suggestions
            var suggestedQuestions = await _autoSuggestService.GenerateSuggestionsAsync(question, answer, documentSnippets);

            var faqAnswer = new FAQAnswer
            {
                Question = question,
                Answer = answer,
                Source = hasSufficientContext ? "AI Assistant" : "System",
                SourceDocuments = retrievalResults.Select(r => r.Chunk.SourceDocument).Distinct().ToList(),
                DocumentSnippets = documentSnippets,
                SuggestedQuestions = suggestedQuestions
            };

            if (!string.IsNullOrEmpty(conversationId))
            {
                await _conversationService.AddMessageAsync(conversationId, "User", question);
                await _conversationService.AddMessageAsync(conversationId, "AI", answer, question, answer);
            }

            return faqAnswer;
        }
    }
}