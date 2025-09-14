using AskMeNow.Application.Services;
using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text.RegularExpressions;

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
        
        // Check if the question is a greeting or small talk
        if (IsGreetingOrSmallTalk(sanitizedQuestion))
        {
            return new FAQAnswer
            {
                Question = sanitizedQuestion,
                Answer = GetGreetingResponse(sanitizedQuestion),
                Source = "AI Assistant"
            };
        }
        
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

    private bool IsGreetingOrSmallTalk(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return false;

        // Convert to lowercase for case-insensitive matching
        var lowerQuestion = question.ToLowerInvariant().Trim();
        
        // Remove extra whitespace and punctuation for better matching
        var normalizedQuestion = Regex.Replace(lowerQuestion, @"[^\w\s]", "").Trim();
        var words = normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Define greeting patterns
        var greetingPatterns = new[]
        {
            // Basic greetings
            "hi", "hello", "hey", "hiya", "howdy",
            
            // Time-based greetings
            "good morning", "good afternoon", "good evening", "good night",
            "morning", "afternoon", "evening",
            
            // How are you variations
            "how are you", "how are you doing", "how do you do", "how's it going",
            "what's up", "whats up", "sup", "howdy",
            
            // Casual greetings
            "hey there", "hi there", "hello there",
            "hi bot", "hello bot", "hey bot",
            "hi assistant", "hello assistant", "hey assistant",
            
            // Simple acknowledgments
            "thanks", "thank you", "bye", "goodbye", "see you later"
        };

        // Check for exact matches
        foreach (var pattern in greetingPatterns)
        {
            if (normalizedQuestion == pattern || normalizedQuestion.StartsWith(pattern + " "))
                return true;
        }

        // Check for single word greetings
        if (words.Length == 1)
        {
            var singleWordGreetings = new[] { "hi", "hello", "hey", "hiya", "howdy", "morning", "afternoon", "evening", "thanks", "bye" };
            if (singleWordGreetings.Contains(words[0]))
                return true;
        }

        // Check for "how are you" variations
        if (words.Length >= 2 && words[0] == "how" && (words[1] == "are" || words[1] == "do"))
            return true;

        // Check for "what's up" variations
        if (words.Length >= 2 && words[0] == "what" && words[1] == "up")
            return true;

        return false;
    }

    private string GetGreetingResponse(string question)
    {
        var lowerQuestion = question.ToLowerInvariant().Trim();
        var normalizedQuestion = Regex.Replace(lowerQuestion, @"[^\w\s]", "").Trim();
        
        // Time-based responses
        var currentHour = DateTime.Now.Hour;
        if (normalizedQuestion.Contains("morning") || (currentHour >= 5 && currentHour < 12))
        {
            return "Good morning! Ready to answer your questions.";
        }
        else if (normalizedQuestion.Contains("afternoon") || (currentHour >= 12 && currentHour < 17))
        {
            return "Good afternoon! How can I help you today?";
        }
        else if (normalizedQuestion.Contains("evening") || normalizedQuestion.Contains("night") || (currentHour >= 17 || currentHour < 5))
        {
            return "Good evening! I'm here to assist you with your questions.";
        }
        
        // How are you responses
        if (normalizedQuestion.Contains("how are you") || normalizedQuestion.Contains("how do you do"))
        {
            return "I'm doing great, thank you! How can I assist you today?";
        }
        
        // What's up responses
        if (normalizedQuestion.Contains("what up") || normalizedQuestion.Contains("whats up") || normalizedQuestion.Contains("sup"))
        {
            return "Not much! Just ready to help you with any questions you might have.";
        }
        
        // Thanks responses
        if (normalizedQuestion.Contains("thank") || normalizedQuestion.Contains("thanks"))
        {
            return "You're welcome! Is there anything else I can help you with?";
        }
        
        // Goodbye responses
        if (normalizedQuestion.Contains("bye") || normalizedQuestion.Contains("goodbye") || normalizedQuestion.Contains("see you"))
        {
            return "Goodbye! Feel free to come back anytime if you have more questions.";
        }
        
        // Default greeting response
        return "Hello! How can I help you today?";
    }
}
