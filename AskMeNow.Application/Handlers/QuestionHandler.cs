using AskMeNow.Application.Services;
using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Application.Handlers
{
    public class QuestionHandler : IQuestionHandler
    {
        private readonly IFAQService _faqService;
        private readonly IDocumentCacheService _documentCacheService;
        private readonly ISentimentAnalysisService _sentimentAnalysisService;
        private readonly ISmallTalkService _smallTalkService;

        public QuestionHandler(
            IFAQService faqService,
            IDocumentCacheService documentCacheService,
            ISentimentAnalysisService sentimentAnalysisService,
            ISmallTalkService smallTalkService)
        {
            _faqService = faqService;
            _documentCacheService = documentCacheService;
            _sentimentAnalysisService = sentimentAnalysisService;
            _smallTalkService = smallTalkService;
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

            var sanitizedQuestion = question.Trim();

            // Analyze sentiment and intent
            var analysis = await _sentimentAnalysisService.AnalyzeAsync(sanitizedQuestion);

            return await RouteMessageAsync(sanitizedQuestion, analysis, null);
        }

        public async Task<FAQAnswer> ProcessQuestionAsync(string question, string conversationId)
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

            var sanitizedQuestion = question.Trim();

            // Analyze sentiment and intent
            var analysis = await _sentimentAnalysisService.AnalyzeAsync(sanitizedQuestion);

            return await RouteMessageAsync(sanitizedQuestion, analysis, conversationId);
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

        private async Task<FAQAnswer> RouteMessageAsync(string question, SentimentAnalysisResult analysis, string? conversationId)
        {
            if (_smallTalkService.CanHandle(analysis))
            {
                var response = await _smallTalkService.GetResponseAsync(question, analysis);
                return new FAQAnswer
                {
                    Question = question,
                    Answer = response,
                    Source = "AI Assistant"
                };
            }

            // Handle questions and complaints with document search
            var faqAnswer = conversationId != null
                ? await _faqService.AnswerQuestionAsync(question, conversationId)
                : await _faqService.AnswerQuestionAsync(question);

            // Add empathetic response for complaints with negative sentiment
            if (analysis.Intent == Intent.Complaint && analysis.Sentiment == Sentiment.Negative)
            {
                faqAnswer.Answer = PrependEmpatheticResponse(faqAnswer.Answer);
            }

            return faqAnswer;
        }

        private string PrependEmpatheticResponse(string originalAnswer)
        {
            var empatheticResponses = new[]
            {
            "I understand your frustration and I'm sorry you're experiencing this issue. ",
            "I can see this is really bothering you, and I want to help resolve this. ",
            "I'm sorry to hear about this problem. Let me help you find a solution. ",
            "I understand how frustrating this must be. Here's what I can tell you: ",
            "I'm sorry you're having this issue. I'm here to help you resolve it. "
        };

            var random = new Random();
            var empatheticPrefix = empatheticResponses[random.Next(empatheticResponses.Length)];

            return empatheticPrefix + originalAnswer;
        }
    }
}

