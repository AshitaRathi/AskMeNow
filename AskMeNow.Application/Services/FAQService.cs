using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Application.Services;

public interface IFAQService
{
    Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath);
    Task<FAQAnswer> AnswerQuestionAsync(string question);
}

public class FAQService : IFAQService
{
    private readonly IDocumentCacheService _documentCacheService;
    private readonly IBedrockClientService _bedrockClientService;
    private readonly IDocumentChunkingService _chunkingService;

    public FAQService(IDocumentCacheService documentCacheService, IBedrockClientService bedrockClientService, IDocumentChunkingService chunkingService)
    {
        _documentCacheService = documentCacheService;
        _bedrockClientService = bedrockClientService;
        _chunkingService = chunkingService;
    }

    public async Task<List<FAQDocument>> LoadDocumentsAsync(string folderPath)
    {
        return await _documentCacheService.LoadAndCacheDocumentsAsync(folderPath);
    }

    public async Task<FAQAnswer> AnswerQuestionAsync(string question)
    {
        var context = _documentCacheService.GetRelevantContentForQuestion(question);
        
        if (string.IsNullOrEmpty(context))
        {
            return new FAQAnswer
            {
                Question = question,
                Answer = "No documents have been loaded. Please select a folder with .txt files first.",
                Source = "System"
            };
        }

        // Get document snippets for source references
        var documents = _documentCacheService.GetCachedDocuments();
        var documentSnippets = _chunkingService.GetRelevantSnippets(question, documents, 5);

        var answer = await _bedrockClientService.GenerateAnswerAsync(question, context);

        return new FAQAnswer
        {
            Question = question,
            Answer = answer,
            Source = "AI Assistant",
            SourceDocuments = new List<string> { "Knowledge Base" },
            DocumentSnippets = documentSnippets
        };
    }
}
