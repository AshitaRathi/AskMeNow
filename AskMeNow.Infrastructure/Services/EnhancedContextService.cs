using AskMeNow.Core.Interfaces;
using System.Text;

namespace AskMeNow.Infrastructure.Services
{
    public class EnhancedContextService : IEnhancedContextService
    {
        private readonly ContextConfiguration _config;

        public EnhancedContextService()
        {
            _config = new ContextConfiguration();
        }

        public async Task<string> BuildContextAsync(
            string query,
            List<RetrievalResult> retrievalResults,
            string? conversationContext = null,
            List<FallbackSuggestion>? fallbackSuggestions = null)
        {
            var contextBuilder = new StringBuilder();

            contextBuilder.AppendLine(_config.SystemPrompt);
            contextBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(conversationContext) && _config.IncludeConversationHistory)
            {
                contextBuilder.AppendLine("Previous conversation context:");
                contextBuilder.AppendLine(conversationContext);
                contextBuilder.AppendLine();
            }

            if (retrievalResults.Any())
            {
                contextBuilder.AppendLine("Relevant Document Chunks:");
                contextBuilder.AppendLine();

                var chunksToInclude = retrievalResults
                    .OrderByDescending(r => r.SimilarityScore)
                    .Take(_config.MaxChunksToInclude)
                    .ToList();

                for (int i = 0; i < chunksToInclude.Count; i++)
                {
                    var result = chunksToInclude[i];
                    var chunk = result.Chunk;

                    contextBuilder.AppendLine($"[Chunk {i + 1}]");

                    if (chunk.Headers.Any())
                    {
                        contextBuilder.AppendLine($"Context: {string.Join(" > ", chunk.Headers)}");
                    }

                    contextBuilder.AppendLine($"Source: {chunk.SourceDocument}");
                    contextBuilder.AppendLine($"Relevance: {result.SimilarityScore:F2}");
                    contextBuilder.AppendLine();
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine();
                }
            }

            if (!HasSufficientContext(retrievalResults) && fallbackSuggestions?.Any() == true)
            {
                contextBuilder.AppendLine("Available Topics (no direct match found):");
                contextBuilder.AppendLine();

                foreach (var suggestion in fallbackSuggestions.Take(5))
                {
                    contextBuilder.AppendLine($"- {suggestion.Topic}: {suggestion.Description}");
                }
                contextBuilder.AppendLine();
            }

            contextBuilder.AppendLine($"User Question: {query}");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Answer the user's question using only the information provided above.");

            if (!HasSufficientContext(retrievalResults))
            {
                contextBuilder.AppendLine("If the information above doesn't contain enough detail to answer the question, say: \"Not available in loaded documents.\"");
            }

            return await Task.FromResult(contextBuilder.ToString());
        }

        public bool HasSufficientContext(List<RetrievalResult> retrievalResults, float threshold = 0.3f)
        {
            if (!retrievalResults.Any())
                return false;

            var hasGoodMatch = retrievalResults.Any(r => r.SimilarityScore >= threshold);

            var decentMatches = retrievalResults.Count(r => r.SimilarityScore >= 0.2f);
            var hasMultipleMatches = decentMatches >= 2;

            return hasGoodMatch || hasMultipleMatches;
        }

        public string FormatFallbackResponse(string query, List<FallbackSuggestion> fallbackSuggestions, bool hasDocuments)
        {
            if (!hasDocuments)
            {
                return "No documents have been loaded. Please select a folder containing documents first.";
            }

            if (!fallbackSuggestions.Any())
            {
                return "I couldn't find information related to your question in the loaded documents. Please try rephrasing your question or ask about a different topic.";
            }

            var response = new StringBuilder();
            response.AppendLine("Couldn't find an exact answer to your question. Do you mean one of these related topics:");
            response.AppendLine();

            foreach (var suggestion in fallbackSuggestions.Take(5))
            {
                response.AppendLine($"• **{suggestion.Topic}** - {suggestion.Description}");

                if (suggestion.RelatedChunks.Any())
                {
                    response.AppendLine($"  *Available in: {suggestion.SourceDocument}*");
                }
            }

            response.AppendLine();
            response.AppendLine("Please ask a more specific question about any of these topics, or try rephrasing your original question.");

            return response.ToString();
        }

        public List<string> ExtractKeyTopics(List<RetrievalResult> retrievalResults, int maxTopics = 5)
        {
            var topics = new Dictionary<string, int>();

            foreach (var result in retrievalResults)
            {
                var chunk = result.Chunk;

                foreach (var header in chunk.Headers)
                {
                    var words = ExtractKeywords(header);
                    foreach (var word in words)
                    {
                        topics[word] = topics.GetValueOrDefault(word, 0) + 1;
                    }
                }

                var contentSample = chunk.Content.Substring(0, Math.Min(200, chunk.Content.Length));
                var contentWords = ExtractKeywords(contentSample);

                foreach (var word in contentWords.Take(10))
                {
                    topics[word] = topics.GetValueOrDefault(word, 0) + 1;
                }
            }

            return topics
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxTopics)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private List<string> ExtractKeywords(string text)
        {
            var commonWords = new HashSet<string>
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did",
            "will", "would", "could", "should", "may", "might", "can", "what", "how", "when", "where", "why", "who",
            "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they",
            "about", "into", "through", "during", "before", "after", "above", "below", "up", "down", "out", "off", "over", "under"
        };

            return text.Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' },
                             StringSplitOptions.RemoveEmptyEntries)
                       .Where(word => word.Length > 2 && !commonWords.Contains(word.ToLowerInvariant()))
                       .Select(word => word.ToLowerInvariant())
                       .ToList();
        }
    }
}
