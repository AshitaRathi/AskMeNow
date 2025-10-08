using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    public interface IDocumentChunkingService
    {
        List<DocumentChunk> ChunkDocuments(List<FAQDocument> documents);
        List<DocumentChunk> FindRelevantChunks(string question, List<DocumentChunk> chunks, int maxChunks = 5);
        List<DocumentSnippet> GetRelevantSnippets(string question, List<FAQDocument> documents, int maxSnippets = 5);
    }

    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string SourceDocument { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public double RelevanceScore { get; set; }
    }
}
