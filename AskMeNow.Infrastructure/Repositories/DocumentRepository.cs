using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;

namespace AskMeNow.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private List<FAQDocument> _documents = new();
    private string _allContent = string.Empty;

    public async Task<List<FAQDocument>> LoadDocumentsFromFolderAsync(string folderPath)
    {
        _documents.Clear();
        _allContent = string.Empty;

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        var txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories);
        var documents = new List<FAQDocument>();

        foreach (var filePath in txtFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                var document = new FAQDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = fileName,
                    Content = content,
                    FilePath = filePath
                };

                documents.Add(document);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other files
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
            }
        }

        _documents = documents;
        _allContent = string.Join("\n\n", documents.Select(d => $"Document: {d.Title}\n{d.Content}"));

        return documents;
    }

    public string GetAllContent()
    {
        return _allContent;
    }
}
