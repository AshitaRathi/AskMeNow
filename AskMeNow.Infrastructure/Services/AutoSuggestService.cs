using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text.Json;

namespace AskMeNow.Infrastructure.Services
{
    public class AutoSuggestService : IAutoSuggestService
    {
        private readonly IBedrockClientService _bedrockClientService;
        private readonly IConversationService _conversationService;

        public AutoSuggestService(IBedrockClientService bedrockClientService, IConversationService conversationService)
        {
            _bedrockClientService = bedrockClientService;
            _conversationService = conversationService;
        }

        public async Task<List<SuggestedQuestion>> GenerateSuggestionsAsync(string question, string answer, List<DocumentSnippet>? documentSnippets = null)
        {
            try
            {
                var prompt = BuildSuggestionPrompt(question, answer, documentSnippets);
                var suggestions = await _bedrockClientService.GenerateSuggestionsAsync(prompt);

                return ParseSuggestions(suggestions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating suggestions: {ex.Message}");
                return GetFallbackSuggestions(question, answer);
            }
        }

        public async Task<List<SuggestedQuestion>> GenerateContextualSuggestionsAsync(string conversationId, string currentAnswer)
        {
            try
            {
                var chatHistory = await _conversationService.GetChatHistoryAsync(conversationId, 3);
                var context = string.Join("\n", chatHistory.Select(m => $"{m.Sender}: {m.Content}"));

                var prompt = BuildContextualSuggestionPrompt(context, currentAnswer);
                var suggestions = await _bedrockClientService.GenerateSuggestionsAsync(prompt);

                return ParseSuggestions(suggestions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating contextual suggestions: {ex.Message}");
                return GetFallbackSuggestions("", currentAnswer);
            }
        }

        private string BuildSuggestionPrompt(string question, string answer, List<DocumentSnippet>? documentSnippets)
        {
            var prompt = $@"Based on the following Q&A exchange, generate 3 relevant follow-up questions that a user might ask. 
The questions should be natural, helpful, and build upon the information provided.

Original Question: {question}

AI Answer: {answer}";

            if (documentSnippets?.Any() == true)
            {
                prompt += "\n\nRelevant document snippets:\n";
                foreach (var snippet in documentSnippets.Take(3))
                {
                    prompt += $"- From {snippet.FileName}: {snippet.SnippetText.Substring(0, Math.Min(200, snippet.SnippetText.Length))}...\n";
                }
            }

            prompt += @"

Generate exactly 3 follow-up questions that are:
1. Natural and conversational
2. Build upon the current answer
3. Help the user explore related topics
4. Are specific and actionable

Format your response as a JSON array of strings:
[""Question 1"", ""Question 2"", ""Question 3""]";

            return prompt;
        }

        private string BuildContextualSuggestionPrompt(string chatHistory, string currentAnswer)
        {
            var prompt = $@"Based on the following conversation history and the latest AI response, generate 3 relevant follow-up questions.

Conversation History:
{chatHistory}

Latest AI Response: {currentAnswer}

Generate exactly 3 follow-up questions that:
1. Take into account the conversation context
2. Build naturally on the latest response
3. Help continue the conversation meaningfully
4. Are specific and relevant to the topic being discussed

Format your response as a JSON array of strings:
[""Question 1"", ""Question 2"", ""Question 3""]";

            return prompt;
        }

        private List<SuggestedQuestion> ParseSuggestions(string suggestions)
        {
            try
            {
                var jsonArray = JsonSerializer.Deserialize<string[]>(suggestions);
                if (jsonArray != null && jsonArray.Length > 0)
                {
                    return jsonArray.Select((q, index) => new SuggestedQuestion
                    {
                        Question = q.Trim(),
                        RelevanceScore = 1.0 - (index * 0.1),
                        Category = "follow-up"
                    }).ToList();
                }
            }
            catch (JsonException)
            {
                return ExtractQuestionsFromText(suggestions);
            }

            return GetFallbackSuggestions("", "");
        }

        private List<SuggestedQuestion> ExtractQuestionsFromText(string text)
        {
            var questions = new List<SuggestedQuestion>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || trimmedLine.StartsWith("*"))
                {
                    trimmedLine = trimmedLine.Substring(1).Trim();
                }

                if (trimmedLine.EndsWith("?") && trimmedLine.Length > 10)
                {
                    questions.Add(new SuggestedQuestion
                    {
                        Question = trimmedLine,
                        RelevanceScore = 1.0 - (questions.Count * 0.1),
                        Category = "follow-up"
                    });
                }
            }

            return questions.Take(3).ToList();
        }

        private List<SuggestedQuestion> GetFallbackSuggestions(string question, string answer)
        {
            var suggestions = new List<SuggestedQuestion>();

            if (!string.IsNullOrEmpty(answer))
            {
                if (answer.ToLower().Contains("price") || answer.ToLower().Contains("cost"))
                {
                    suggestions.Add(new SuggestedQuestion
                    {
                        Question = "Are there any discounts or special offers available?",
                        RelevanceScore = 0.9,
                        Category = "follow-up"
                    });
                }

                if (answer.ToLower().Contains("feature") || answer.ToLower().Contains("include"))
                {
                    suggestions.Add(new SuggestedQuestion
                    {
                        Question = "What are the main benefits of this?",
                        RelevanceScore = 0.8,
                        Category = "follow-up"
                    });
                }

                suggestions.Add(new SuggestedQuestion
                {
                    Question = "Can you provide more details about this?",
                    RelevanceScore = 0.7,
                    Category = "follow-up"
                });
            }

            while (suggestions.Count < 3)
            {
                suggestions.Add(new SuggestedQuestion
                {
                    Question = "What else should I know about this topic?",
                    RelevanceScore = 0.6 - (suggestions.Count * 0.1),
                    Category = "follow-up"
                });
            }

            return suggestions.Take(3).ToList();
        }
    }
}
