using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces
{
    public interface IDocumentCacheService
    {
        Task<List<FAQDocument>> LoadAndCacheDocumentsAsync(string folderPath);
        string GetCachedContent();
        string GetRelevantContentForQuestion(string question);
        List<FAQDocument> GetCachedDocuments();
        FileProcessingResult? GetLastProcessingResult();
        void ClearCache();
        bool IsCacheValid { get; }
    }
}
