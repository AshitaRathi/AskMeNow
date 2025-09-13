using AskMeNow.Core.Entities;

namespace AskMeNow.Core.Interfaces;

public interface IDocumentParserService
{
    Task<List<FAQDocument>> ParseDocumentsFromFolderAsync(string folderPath);
    Task<FAQDocument> ParseDocumentAsync(string filePath);
    List<string> GetSupportedExtensions();
    Task<FileProcessingResult> GetFileProcessingStatsAsync(string folderPath);
}

public class FileProcessingResult
{
    public int SupportedFilesFound { get; set; }
    public int UnsupportedFilesFound { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int FailedToProcess { get; set; }
    public List<string> SupportedExtensions { get; set; } = new();
    public List<string> UnsupportedExtensions { get; set; } = new();
}
