using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    /// <summary>
    /// Enhanced retrieval service with multi-query expansion and improved similarity search
    /// </summary>
    public interface IEnhancedRetrievalService
    {
        /// <summary>
        /// Retrieves relevant chunks using semantic similarity with multi-query expansion
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="maxChunks">Maximum number of chunks to return</param>
        /// <param name="minSimilarityThreshold">Minimum similarity threshold (0.0 - 1.0)</param>
        /// <returns>List of relevant chunks with similarity scores</returns>
        Task<List<RetrievalResult>> RetrieveRelevantChunksAsync(string query, int maxChunks = 10, float minSimilarityThreshold = 0.1f);

        /// <summary>
        /// Generates expanded queries for vague or broad questions
        /// </summary>
        /// <param name="originalQuery">Original user query</param>
        /// <returns>List of expanded queries including synonyms and related terms</returns>
        Task<List<ExpandedQuery>> GenerateExpandedQueriesAsync(string originalQuery);

        /// <summary>
        /// Validates that embeddings are working correctly by running test queries
        /// </summary>
        /// <param name="testQueries">Optional test queries, if null uses default tests</param>
        /// <returns>Validation result with success status and details</returns>
        Task<EmbeddingValidationResult> ValidateEmbeddingsAsync(List<string>? testQueries = null);

        /// <summary>
        /// Gets fallback suggestions when similarity scores are too low
        /// </summary>
        /// <param name="query">Original query</param>
        /// <param name="maxSuggestions">Maximum number of suggestions</param>
        /// <returns>List of related topics or suggestions</returns>
        Task<List<FallbackSuggestion>> GetFallbackSuggestionsAsync(string query, int maxSuggestions = 5);
    }

    /// <summary>
    /// Result of a retrieval operation with metadata
    /// </summary>
    public class RetrievalResult
    {
        public SemanticChunk Chunk { get; set; } = new();
        public float SimilarityScore { get; set; }
        public string SourceQuery { get; set; } = string.Empty; // Which query led to this result
        public bool IsFromExpandedQuery { get; set; }
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Expanded query with metadata
    /// </summary>
    public class ExpandedQuery
    {
        public string Query { get; set; } = string.Empty;
        public QueryType Type { get; set; }
        public float Weight { get; set; } = 1.0f; // Weight for combining results
        public string Reason { get; set; } = string.Empty; // Why this query was generated
    }

    /// <summary>
    /// Types of expanded queries
    /// </summary>
    public enum QueryType
    {
        Original,
        Synonym,
        Related,
        Broader,
        Narrower,
        Contextual
    }

    /// <summary>
    /// Result of embedding validation
    /// </summary>
    public class EmbeddingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> TestResults { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalEmbeddings { get; set; }
        public int TestedEmbeddings { get; set; }
        public float AverageSimilarity { get; set; }
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Fallback suggestion when no good matches found
    /// </summary>
    public class FallbackSuggestion
    {
        public string Topic { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourceDocument { get; set; } = string.Empty;
        public float RelevanceScore { get; set; }
        public List<string> RelatedChunks { get; set; } = new();
    }

}