using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    public interface IDocumentPreviewService
    {
        Task<DocumentPreview?> GetDocumentPreviewAsync(string filePath);
        Task<List<DocumentPreview>> GetAllDocumentPreviewsAsync();
        Task<List<DocumentHighlight>> GetHighlightsForDocumentAsync(string filePath);
        Task UpdateHighlightsAsync(string filePath, List<DocumentSnippet> referencedSnippets);
    }
}
