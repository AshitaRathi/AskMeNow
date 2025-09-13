using AskMeNow.Application.Services;
using AskMeNow.Core.Entities;

namespace AskMeNow.Application.Handlers;

public interface IQuestionHandler
{
    Task<FAQAnswer> ProcessQuestionAsync(string question);
    Task<List<FAQDocument>> InitializeDocumentsAsync(string folderPath);
}

public class QuestionHandler : IQuestionHandler
{
    private readonly IFAQService _faqService;

    public QuestionHandler(IFAQService faqService)
    {
        _faqService = faqService;
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
}
