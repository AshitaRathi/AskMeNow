using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly IDocumentParserService _parserService;
    private List<FAQDocument> _documents = new();
    private string _allContent = string.Empty;
    private FileProcessingResult? _lastProcessingResult;

    public DocumentRepository(IDocumentParserService parserService)
    {
        _parserService = parserService;
    }

    public async Task<List<FAQDocument>> LoadDocumentsFromFolderAsync(string folderPath)
    {
        _documents.Clear();
        _allContent = string.Empty;

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        // Get file processing statistics
        _lastProcessingResult = await _parserService.GetFileProcessingStatsAsync(folderPath);

        // Use the parser service to load all supported document types
        var documents = await _parserService.ParseDocumentsFromFolderAsync(folderPath);

        _documents = documents;
        _allContent = string.Join("\n\n", documents.Select(d => $"Document: {d.Title} ({Path.GetExtension(d.FilePath)})\n{d.Content}"));

        return documents;
    }

    public string GetAllContent()
    {
        return _allContent;
    }

    public FileProcessingResult? GetLastProcessingResult()
    {
        return _lastProcessingResult;
    }
}
