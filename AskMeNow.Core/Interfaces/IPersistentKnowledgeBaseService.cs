using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces;

public interface IPersistentKnowledgeBaseService
{
    Task InitializeAsync();
    Task ProcessDocumentAsync(string filePath);
    Task ProcessDocumentsInFolderAsync(string folderPath);
    Task<List<DocumentSnippet>> FindRelevantSnippetsAsync(string question, int maxSnippets = 5);
    Task DeleteDocumentAsync(string filePath);
    Task UpdateDocumentAsync(string filePath);
    Task<List<DocumentEntity>> GetAllDocumentsAsync();
    Task<bool> IsDocumentProcessedAsync(string filePath);
    Task<DateTime?> GetDocumentLastProcessedAsync(string filePath);
}
