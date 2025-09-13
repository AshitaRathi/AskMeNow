using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Collections.Concurrent;

namespace AskMeNow.Infrastructure.Services;

public class DocumentCacheService : IDocumentCacheService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkingService _chunkingService;
    private readonly ConcurrentDictionary<string, List<FAQDocument>> _documentCache = new();
    private readonly ConcurrentDictionary<string, string> _contentCache = new();
    private readonly ConcurrentDictionary<string, List<DocumentChunk>> _chunkCache = new();
    private string _currentFolderPath = string.Empty;

    public DocumentCacheService(IDocumentRepository documentRepository, IDocumentChunkingService chunkingService)
    {
        _documentRepository = documentRepository;
        _chunkingService = chunkingService;
    }

    public bool IsCacheValid => !string.IsNullOrEmpty(_currentFolderPath) && _documentCache.ContainsKey(_currentFolderPath);

    public async Task<List<FAQDocument>> LoadAndCacheDocumentsAsync(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));

        // Check if we already have cached documents for this folder
        if (_documentCache.TryGetValue(folderPath, out var cachedDocuments))
        {
            _currentFolderPath = folderPath;
            return cachedDocuments;
        }

        // Load documents from repository
        var documents = await _documentRepository.LoadDocumentsFromFolderAsync(folderPath);
        
        // Cache the documents
        _documentCache.TryAdd(folderPath, documents);
        
        // Create chunks for better RAG
        var chunks = _chunkingService.ChunkDocuments(documents);
        _chunkCache.TryAdd(folderPath, chunks);
        
        // Cache the combined content
        var combinedContent = string.Join("\n\n", documents.Select(d => $"Document: {d.Title}\n{d.Content}"));
        _contentCache.TryAdd(folderPath, combinedContent);
        
        _currentFolderPath = folderPath;
        
        return documents;
    }

    public string GetCachedContent()
    {
        if (string.IsNullOrEmpty(_currentFolderPath))
            return string.Empty;

        return _contentCache.TryGetValue(_currentFolderPath, out var content) ? content : string.Empty;
    }

    public string GetRelevantContentForQuestion(string question)
    {
        if (string.IsNullOrEmpty(_currentFolderPath) || string.IsNullOrWhiteSpace(question))
            return GetCachedContent();

        if (_chunkCache.TryGetValue(_currentFolderPath, out var chunks))
        {
            var relevantChunks = _chunkingService.FindRelevantChunks(question, chunks, 5);
            if (relevantChunks.Any())
            {
                return string.Join("\n\n", relevantChunks.Select(c => $"From {c.SourceDocument}:\n{c.Content}"));
            }
        }

        // Fallback to full content if no relevant chunks found
        return GetCachedContent();
    }

    public void ClearCache()
    {
        _documentCache.Clear();
        _contentCache.Clear();
        _chunkCache.Clear();
        _currentFolderPath = string.Empty;
    }
}
