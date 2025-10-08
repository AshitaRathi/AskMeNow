using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    /// <summary>
    /// Service for building enhanced context with proper chunk injection and fallback handling
    /// </summary>
    public interface IEnhancedContextService
    {
        /// <summary>
        /// Builds context for LLM with retrieved chunks and conversation history
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="retrievalResults">Retrieved chunks with similarity scores</param>
        /// <param name="conversationContext">Previous conversation context</param>
        /// <param name="fallbackSuggestions">Fallback suggestions if similarity is low</param>
        /// <returns>Formatted context for LLM</returns>
        Task<string> BuildContextAsync(
            string query,
            List<RetrievalResult> retrievalResults,
            string? conversationContext = null,
            List<FallbackSuggestion>? fallbackSuggestions = null);

        /// <summary>
        /// Determines if the retrieved chunks have sufficient similarity for a good answer
        /// </summary>
        /// <param name="retrievalResults">Retrieved chunks</param>
        /// <param name="threshold">Similarity threshold (default 0.3)</param>
        /// <returns>True if chunks are sufficient for answering</returns>
        bool HasSufficientContext(List<RetrievalResult> retrievalResults, float threshold = 0.3f);

        /// <summary>
        /// Formats fallback response when no good context is available
        /// </summary>
        /// <param name="query">Original query</param>
        /// <param name="fallbackSuggestions">Available suggestions</param>
        /// <param name="hasDocuments">Whether any documents are loaded</param>
        /// <returns>Formatted fallback response</returns>
        string FormatFallbackResponse(string query, List<FallbackSuggestion> fallbackSuggestions, bool hasDocuments);

        /// <summary>
        /// Extracts key topics from retrieval results for suggestion generation
        /// </summary>
        /// <param name="retrievalResults">Retrieved chunks</param>
        /// <param name="maxTopics">Maximum number of topics to extract</param>
        /// <returns>List of key topics</returns>
        List<string> ExtractKeyTopics(List<RetrievalResult> retrievalResults, int maxTopics = 5);
    }

    /// <summary>
    /// Configuration for context building
    /// </summary>
    public class ContextConfiguration
    {
        public int MaxContextTokens { get; set; } = 8000;
        public int MaxChunksToInclude { get; set; } = 10;
        public float MinSimilarityThreshold { get; set; } = 0.1f;
        public bool IncludeConversationHistory { get; set; } = true;
        public int MaxConversationTurns { get; set; } = 5;
        public bool EnableFallbackSuggestions { get; set; } = true;
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant that answers questions based on the provided context. Please provide a clear, accurate, and helpful answer based on the information given. If the context doesn't contain enough information to answer the question, please say so.";
    }

}