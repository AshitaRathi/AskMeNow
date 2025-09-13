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

    public FAQService(IDocumentCacheService documentCacheService, IBedrockClientService bedrockClientService)
    {
        _documentCacheService = documentCacheService;
        _bedrockClientService = bedrockClientService;
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

        var answer = await _bedrockClientService.GenerateAnswerAsync(question, context);

        return new FAQAnswer
        {
            Question = question,
            Answer = answer,
            Source = "AI Assistant",
            SourceDocuments = new List<string> { "Knowledge Base" }
        };
    }
}
