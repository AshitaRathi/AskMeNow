using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    /// <summary>
    /// Service for semantic-aware document chunking with proper token limits and overlap
    /// </summary>
    public interface ISemanticChunkingService
    {
        /// <summary>
        /// Chunks a document using semantic boundaries (paragraphs, headings) with token limits
        /// </summary>
        /// <param name="content">Document content to chunk</param>
        /// <param name="fileName">Name of the source file</param>
        /// <param name="filePath">Full path to the source file</param>
        /// <returns>List of semantic chunks</returns>
        Task<List<SemanticChunk>> ChunkDocumentAsync(string content, string fileName, string filePath);

        /// <summary>
        /// Estimates token count for text (approximate)
        /// </summary>
        /// <param name="text">Text to count tokens for</param>
        /// <returns>Estimated token count</returns>
        int EstimateTokenCount(string text);

        /// <summary>
        /// Splits text at semantic boundaries (paragraphs, headings, sentences)
        /// </summary>
        /// <param name="text">Text to split</param>
        /// <returns>List of semantic segments</returns>
        List<SemanticSegment> SplitIntoSemanticSegments(string text);
    }

    /// <summary>
    /// Represents a semantic chunk with metadata
    /// </summary>
    public class SemanticChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string SourceDocument { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int TokenCount { get; set; }
        public ChunkType Type { get; set; } = ChunkType.Paragraph;
        public List<string> Headers { get; set; } = new(); // Hierarchical headers for context
        public double RelevanceScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a semantic segment of text
    /// </summary>
    public class SemanticSegment
    {
        public string Content { get; set; } = string.Empty;
        public SegmentType Type { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int TokenCount { get; set; }
        public List<string> Headers { get; set; } = new(); // Parent headers
    }

    /// <summary>
    /// Types of chunks based on content structure
    /// </summary>
    public enum ChunkType
    {
        Paragraph,
        Heading,
        List,
        Table,
        Code,
        Mixed
    }

    /// <summary>
    /// Types of semantic segments
    /// </summary>
    public enum SegmentType
    {
        Paragraph,
        Heading,
        List,
        Table,
        Code,
        Sentence
    }

}