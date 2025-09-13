using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces;

public interface IDocumentRepository
{
    Task<List<FAQDocument>> LoadDocumentsFromFolderAsync(string folderPath);
    string GetAllContent();
    FileProcessingResult? GetLastProcessingResult();
}
