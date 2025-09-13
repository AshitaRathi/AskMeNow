using AskMeNow.Application.Services;
using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Application.Handlers;

public interface IQuestionHandler
{
    Task<FAQAnswer> ProcessQuestionAsync(string question);
    Task<List<FAQDocument>> InitializeDocumentsAsync(string folderPath);
    FileProcessingResult? GetLastProcessingResult();
}

public class QuestionHandler : IQuestionHandler
{
    private readonly IFAQService _faqService;
    private readonly IDocumentCacheService _documentCacheService;

    public QuestionHandler(IFAQService faqService, IDocumentCacheService documentCacheService)
    {
        _faqService = faqService;
        _documentCacheService = documentCacheService;
    }

    public async Task<FAQAnswer> ProcessQuestionAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new FAQAnswer
            {
                Question = question,
                Answer = "Please enter a valid question.",
                Source = "System"
            };
        }

        // Sanitize the question
        var sanitizedQuestion = question.Trim();
        
        return await _faqService.AnswerQuestionAsync(sanitizedQuestion);
    }

    public async Task<List<FAQDocument>> InitializeDocumentsAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        return await _faqService.LoadDocumentsAsync(folderPath);
    }

    public FileProcessingResult? GetLastProcessingResult()
    {
        return _documentCacheService.GetLastProcessingResult();
    }
}
