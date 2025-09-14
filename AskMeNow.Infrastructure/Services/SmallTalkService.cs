using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services;

public class SmallTalkService : ISmallTalkService
{
    private readonly Dictionary<string, string[]> _responseTemplates;

    public SmallTalkService()
    {
        _responseTemplates = InitializeResponseTemplates();
    }

    public bool CanHandle(SentimentAnalysisResult analysis)
    {
        return analysis.Intent == Intent.Greeting || analysis.Intent == Intent.SmallTalk;
    }

    public async Task<string> GetResponseAsync(string userMessage, SentimentAnalysisResult analysis)
    {
        if (!CanHandle(analysis))
        {
            throw new InvalidOperationException("SmallTalkService cannot handle this type of message.");
        }

        var lowerMessage = userMessage.ToLowerInvariant().Trim();
        var normalizedMessage = Regex.Replace(lowerMessage, @"[^\w\s]", "").Trim();

        // Get appropriate response based on intent and sentiment
        var response = analysis.Intent switch
        {
            Intent.Greeting => GetGreetingResponse(normalizedMessage, analysis.Sentiment),
            Intent.SmallTalk => GetSmallTalkResponse(normalizedMessage, analysis.Sentiment),
            _ => GetDefaultResponse(analysis.Sentiment)
        };

        return await Task.FromResult(response);
    }

    private string GetGreetingResponse(string normalizedMessage, Sentiment sentiment)
    {
        var currentHour = DateTime.Now.Hour;
        var responses = new List<string>();

        // Time-based greetings
        if (normalizedMessage.Contains("morning") || (currentHour >= 5 && currentHour < 12))
        {
            responses.AddRange(_responseTemplates["morning_greetings"]);
        }
        else if (normalizedMessage.Contains("afternoon") || (currentHour >= 12 && currentHour < 17))
        {
            responses.AddRange(_responseTemplates["afternoon_greetings"]);
        }
        else if (normalizedMessage.Contains("evening") || normalizedMessage.Contains("night") || (currentHour >= 17 || currentHour < 5))
        {
            responses.AddRange(_responseTemplates["evening_greetings"]);
        }
        else
        {
            responses.AddRange(_responseTemplates["general_greetings"]);
        }

        // Add sentiment-appropriate responses
        if (sentiment == Sentiment.Positive)
        {
            responses.AddRange(_responseTemplates["positive_greetings"]);
        }
        else if (sentiment == Sentiment.Negative)
        {
            responses.AddRange(_responseTemplates["empathetic_greetings"]);
        }

        return GetRandomResponse(responses);
    }

    private string GetSmallTalkResponse(string normalizedMessage, Sentiment sentiment)
    {
        var responses = new List<string>();

        // How are you responses
        if (normalizedMessage.Contains("how are you") || normalizedMessage.Contains("how do you do"))
        {
            responses.AddRange(_responseTemplates["how_are_you"]);
        }
        // What's up responses
        else if (normalizedMessage.Contains("what up") || normalizedMessage.Contains("whats up") || normalizedMessage.Contains("sup"))
        {
            responses.AddRange(_responseTemplates["whats_up"]);
        }
        // Weather-related
        else if (normalizedMessage.Contains("weather") || normalizedMessage.Contains("day") || normalizedMessage.Contains("nice"))
        {
            responses.AddRange(_responseTemplates["weather_smalltalk"]);
        }
        // Weekend/holiday related
        else if (normalizedMessage.Contains("weekend") || normalizedMessage.Contains("holiday") || normalizedMessage.Contains("vacation"))
        {
            responses.AddRange(_responseTemplates["weekend_smalltalk"]);
        }
        // General small talk
        else
        {
            responses.AddRange(_responseTemplates["general_smalltalk"]);
        }

        // Add sentiment-appropriate responses
        if (sentiment == Sentiment.Positive)
        {
            responses.AddRange(_responseTemplates["positive_smalltalk"]);
        }
        else if (sentiment == Sentiment.Negative)
        {
            responses.AddRange(_responseTemplates["empathetic_smalltalk"]);
        }

        return GetRandomResponse(responses);
    }

    private string GetDefaultResponse(Sentiment sentiment)
    {
        var responses = sentiment switch
        {
            Sentiment.Positive => _responseTemplates["positive_greetings"].ToList(),
            Sentiment.Negative => _responseTemplates["empathetic_greetings"].ToList(),
            _ => _responseTemplates["general_greetings"].ToList()
        };

        return GetRandomResponse(responses);
    }

    private string GetRandomResponse(List<string> responses)
    {
        if (responses.Count == 0)
            return "Hello! How can I help you today?";

        var random = new Random();
        return responses[random.Next(responses.Count)];
    }

    private Dictionary<string, string[]> InitializeResponseTemplates()
    {
        return new Dictionary<string, string[]>
        {
            ["morning_greetings"] = new[]
            {
                "Good morning! Ready to help you with any questions you might have.",
                "Good morning! How can I assist you today?",
                "Morning! I'm here and ready to help.",
                "Good morning! What can I help you with today?"
            },
            ["afternoon_greetings"] = new[]
            {
                "Good afternoon! How can I help you today?",
                "Good afternoon! Ready to answer your questions.",
                "Afternoon! What can I assist you with?",
                "Good afternoon! I'm here to help with whatever you need."
            },
            ["evening_greetings"] = new[]
            {
                "Good evening! How can I help you tonight?",
                "Good evening! I'm here to assist you.",
                "Evening! What questions can I answer for you?",
                "Good evening! Ready to help with your inquiries."
            },
            ["general_greetings"] = new[]
            {
                "Hello! How can I help you today?",
                "Hi there! What can I assist you with?",
                "Hello! I'm ready to answer your questions.",
                "Hi! How can I be of service today?",
                "Hello! What would you like to know?"
            },
            ["positive_greetings"] = new[]
            {
                "Great to see you! How can I help today?",
                "Wonderful! I'm excited to assist you.",
                "Fantastic! What can I help you with?",
                "Excellent! Ready to answer your questions."
            },
            ["empathetic_greetings"] = new[]
            {
                "I understand you might be having a tough time. How can I help?",
                "I'm here to support you. What can I assist you with?",
                "I'm sorry to hear you're having difficulties. How can I help?",
                "I'm here to help make things easier for you. What do you need?"
            },
            ["how_are_you"] = new[]
            {
                "I'm doing great, thank you! How can I assist you today?",
                "I'm doing well! Ready to help with your questions.",
                "I'm excellent! What can I help you with?",
                "I'm fantastic! How can I be of service?",
                "I'm doing wonderful! What would you like to know?"
            },
            ["whats_up"] = new[]
            {
                "Not much! Just ready to help you with any questions you might have.",
                "Just here and ready to assist! What can I help you with?",
                "All good! Ready to answer your questions.",
                "Everything's great! How can I help you today?"
            },
            ["weather_smalltalk"] = new[]
            {
                "It's a lovely day! How can I help you?",
                "Beautiful weather! What can I assist you with?",
                "Nice day, isn't it? How can I help you today?",
                "Great weather! What questions can I answer for you?"
            },
            ["weekend_smalltalk"] = new[]
            {
                "Hope you're having a great time! How can I help?",
                "Sounds like fun! What can I assist you with?",
                "That sounds wonderful! How can I help you today?",
                "Enjoy your time! What questions can I answer?"
            },
            ["general_smalltalk"] = new[]
            {
                "That's interesting! How can I help you today?",
                "Sounds good! What can I assist you with?",
                "Nice! How can I be of service?",
                "Great! What would you like to know?"
            },
            ["positive_smalltalk"] = new[]
            {
                "That's wonderful! How can I help you today?",
                "Fantastic! What can I assist you with?",
                "Excellent! How can I be of service?",
                "Amazing! What questions can I answer?"
            },
            ["empathetic_smalltalk"] = new[]
            {
                "I understand. How can I help make things better?",
                "I'm here to support you. What can I assist with?",
                "I'm sorry to hear that. How can I help?",
                "I'm here to help. What do you need assistance with?"
            }
        };
    }
}
