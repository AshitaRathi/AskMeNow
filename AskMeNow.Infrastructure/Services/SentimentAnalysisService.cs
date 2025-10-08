using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services
{
    public class SentimentAnalysisService : ISentimentAnalysisService
    {
        private readonly IBedrockClientService _bedrockService;

        public SentimentAnalysisService(IBedrockClientService bedrockService)
        {
            _bedrockService = bedrockService;
        }

        public async Task<SentimentAnalysisResult> AnalyzeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new SentimentAnalysisResult
                {
                    Sentiment = Sentiment.Neutral,
                    Intent = Intent.Other,
                    Confidence = 0.0,
                    OriginalText = text
                };
            }

            var ruleBasedResult = AnalyzeWithRules(text);
            if (ruleBasedResult.Confidence > 0.8)
            {
                return ruleBasedResult;
            }

            return await AnalyzeWithLLM(text);
        }

        private SentimentAnalysisResult AnalyzeWithRules(string text)
        {
            var lowerText = text.ToLowerInvariant().Trim();
            var normalizedText = Regex.Replace(lowerText, @"[^\w\s]", "").Trim();
            var words = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var intent = DetectIntentWithRules(normalizedText, words);
            var sentiment = DetectSentimentWithRules(normalizedText, words);
            var confidence = CalculateRuleBasedConfidence(intent, sentiment, normalizedText);

            return new SentimentAnalysisResult
            {
                Sentiment = sentiment,
                Intent = intent,
                Confidence = confidence,
                OriginalText = text
            };
        }

        private Intent DetectIntentWithRules(string normalizedText, string[] words)
        {
            var greetingPatterns = new[]
            {
            "hi", "hello", "hey", "hiya", "howdy", "good morning", "good afternoon", "good evening",
            "morning", "afternoon", "evening", "how are you", "how do you do", "whats up", "sup"
        };

            foreach (var pattern in greetingPatterns)
            {
                if (normalizedText.Contains(pattern) || normalizedText.StartsWith(pattern + " "))
                    return Intent.Greeting;
            }

            var smallTalkPatterns = new[]
            {
            "how are you", "how do you do", "whats up", "sup", "how is it going", "how are things",
            "nice day", "beautiful day", "weather", "weekend", "holiday", "vacation"
        };

            foreach (var pattern in smallTalkPatterns)
            {
                if (normalizedText.Contains(pattern))
                    return Intent.SmallTalk;
            }

            var complaintPatterns = new[]
            {
            "problem", "issue", "error", "bug", "broken", "not working", "doesn't work", "failed",
            "terrible", "awful", "horrible", "hate", "angry", "frustrated", "annoyed", "disappointed",
            "complain", "complaint", "wrong", "bad", "sucks", "worst", "useless"
        };

            foreach (var pattern in complaintPatterns)
            {
                if (normalizedText.Contains(pattern))
                    return Intent.Complaint;
            }

            if (normalizedText.Contains("?") ||
                words.Any(w => w == "what" || w == "how" || w == "why" || w == "when" || w == "where" || w == "who" || w == "which"))
            {
                return Intent.Question;
            }

            return Intent.Other;
        }

        private Sentiment DetectSentimentWithRules(string normalizedText, string[] words)
        {
            var positiveWords = new[]
            {
            "good", "great", "excellent", "amazing", "wonderful", "fantastic", "awesome", "perfect",
            "love", "like", "enjoy", "happy", "pleased", "satisfied", "thank", "thanks", "appreciate",
            "helpful", "useful", "brilliant", "outstanding", "superb", "marvelous", "delighted"
        };

            var negativeWords = new[]
            {
            "bad", "terrible", "awful", "horrible", "hate", "dislike", "angry", "frustrated", "annoyed",
            "disappointed", "sad", "upset", "worried", "concerned", "problem", "issue", "error", "bug",
            "broken", "failed", "wrong", "sucks", "worst", "useless", "stupid", "dumb", "ridiculous"
        };

            var positiveCount = words.Count(w => positiveWords.Contains(w));
            var negativeCount = words.Count(w => negativeWords.Contains(w));

            if (negativeCount > positiveCount)
                return Sentiment.Negative;
            if (positiveCount > negativeCount)
                return Sentiment.Positive;

            return Sentiment.Neutral;
        }

        private double CalculateRuleBasedConfidence(Intent intent, Sentiment sentiment, string normalizedText)
        {
            double confidence = 0.5;

            if (intent == Intent.Greeting && (normalizedText.Contains("hello") || normalizedText.Contains("hi")))
                confidence += 0.3;

            if (intent == Intent.Complaint && (normalizedText.Contains("problem") || normalizedText.Contains("issue")))
                confidence += 0.3;

            if (intent == Intent.Question && normalizedText.Contains("?"))
                confidence += 0.2;

            return Math.Min(confidence, 1.0);
        }

        private async Task<SentimentAnalysisResult> AnalyzeWithLLM(string text)
        {
            try
            {
                var prompt = $@"Analyze the following text and return a JSON response with sentiment and intent classification.

Text: ""{text}""

Please classify:
1. Sentiment: Positive, Negative, or Neutral
2. Intent: Greeting, SmallTalk, Question, Complaint, or Other
3. Confidence: A number between 0.0 and 1.0

Return only valid JSON in this format:
{{
    ""sentiment"": ""Positive|Negative|Neutral"",
    ""intent"": ""Greeting|SmallTalk|Question|Complaint|Other"",
    ""confidence"": 0.85
}}";

                var response = await _bedrockService.GenerateAnswerAsync(prompt, "");

                if (string.IsNullOrWhiteSpace(response))
                {
                    return GetFallbackResult(text);
                }

                var jsonResponse = ExtractJsonFromResponse(response);
                var result = JsonSerializer.Deserialize<LLMAnalysisResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    return new SentimentAnalysisResult
                    {
                        Sentiment = ParseSentiment(result.Sentiment),
                        Intent = ParseIntent(result.Intent),
                        Confidence = Math.Max(0.0, Math.Min(1.0, result.Confidence)),
                        OriginalText = text
                    };
                }
            }
            catch (Exception)
            {

            }

            return GetFallbackResult(text);
        }

        private string ExtractJsonFromResponse(string response)
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return response;
        }

        private Sentiment ParseSentiment(string sentiment)
        {
            return sentiment?.ToLowerInvariant() switch
            {
                "positive" => Sentiment.Positive,
                "negative" => Sentiment.Negative,
                "neutral" => Sentiment.Neutral,
                _ => Sentiment.Neutral
            };
        }

        private Intent ParseIntent(string intent)
        {
            return intent?.ToLowerInvariant() switch
            {
                "greeting" => Intent.Greeting,
                "smalltalk" => Intent.SmallTalk,
                "question" => Intent.Question,
                "complaint" => Intent.Complaint,
                "other" => Intent.Other,
                _ => Intent.Other
            };
        }

        private SentimentAnalysisResult GetFallbackResult(string text)
        {
            var lowerText = text.ToLowerInvariant().Trim();
            var normalizedText = Regex.Replace(lowerText, @"[^\w\s]", "").Trim();
            var words = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new SentimentAnalysisResult
            {
                Sentiment = DetectSentimentWithRules(normalizedText, words),
                Intent = DetectIntentWithRules(normalizedText, words),
                Confidence = 0.6,
                OriginalText = text
            };
        }

        private class LLMAnalysisResponse
        {
            public string Sentiment { get; set; } = string.Empty;
            public string Intent { get; set; } = string.Empty;
            public double Confidence { get; set; }
        }
    }
}