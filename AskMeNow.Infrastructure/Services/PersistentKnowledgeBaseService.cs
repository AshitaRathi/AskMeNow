using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;

namespace AskMeNow.Infrastructure.Services;

public class PersistentKnowledgeBaseService : IPersistentKnowledgeBaseService, IDisposable
{
    private readonly KnowledgeBaseContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentParserService _documentParserService;
    private readonly FileSystemWatcher? _fileWatcher;
    private readonly string _watchedFolderPath = string.Empty;
    private bool _disposed = false;

    public PersistentKnowledgeBaseService(
        KnowledgeBaseContext context,
        IEmbeddingService embeddingService,
        IDocumentParserService documentParserService)
    {
        _context = context;
        _embeddingService = embeddingService;
        _documentParserService = documentParserService;
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task ProcessDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var fileInfo = new FileInfo(filePath);
        var existingDocument = await _context.Documents
            .FirstOrDefaultAsync(d => d.FilePath == filePath);

        // Check if document needs processing
        if (existingDocument != null && existingDocument.LastModified >= fileInfo.LastWriteTime)
        {
            return; // Document is up to date
        }

        // Parse document content
        var document = await _documentParserService.ParseDocumentAsync(filePath);
        if (document == null || string.IsNullOrWhiteSpace(document.Content))
            return;

        // Remove existing embeddings if document exists
        if (existingDocument != null)
        {
            _context.Embeddings.RemoveRange(existingDocument.Embeddings);
            _context.Documents.Remove(existingDocument);
        }

        // Create new document entity
        var documentEntity = new DocumentEntity
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            LastModified = fileInfo.LastWriteTime,
            Language = "en"
        };

        _context.Documents.Add(documentEntity);
        await _context.SaveChangesAsync();

        // Split document into chunks and generate embeddings
        var chunks = SplitIntoChunks(document.Content, 500); // 500 character chunks
        var embeddings = new List<EmbeddingEntity>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk);

            var embeddingEntity = new EmbeddingEntity
            {
                DocumentId = documentEntity.Id,
                ChunkIndex = i,
                TextChunk = chunk,
                Vector = ConvertFloatArrayToBytes(embedding),
                VectorDimensions = embedding.Length,
                ModelVersion = _embeddingService.GetModelVersion()
            };

            embeddings.Add(embeddingEntity);
        }

        _context.Embeddings.AddRange(embeddings);
        await _context.SaveChangesAsync();
    }

    public async Task ProcessDocumentsInFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        var txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.AllDirectories);
        
        foreach (var file in txtFiles)
        {
            try
            {
                await ProcessDocumentAsync(file);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }
    }

    public async Task<List<DocumentSnippet>> FindRelevantSnippetsAsync(string question, int maxSnippets = 5)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new List<DocumentSnippet>();

        // Generate embedding for the question
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

        // Get all embeddings with their documents
        var allEmbeddings = await _context.Embeddings
            .Include(e => e.Document)
            .ToListAsync();

        var scoredSnippets = new List<(EmbeddingEntity embedding, float score)>();

        // Calculate similarity scores
        foreach (var embedding in allEmbeddings)
        {
            var storedVector = ConvertBytesToFloatArray(embedding.Vector, embedding.VectorDimensions);
            var similarity = await _embeddingService.CalculateSimilarityAsync(questionEmbedding, storedVector);
            
            if (similarity > 0.1f) // Only include relevant snippets
            {
                scoredSnippets.Add((embedding, similarity));
            }
        }

        // Sort by similarity and take top results
        return scoredSnippets
            .OrderByDescending(x => x.score)
            .Take(maxSnippets)
            .Select(x => new DocumentSnippet
            {
                FileName = x.embedding.Document.FileName,
                SnippetText = x.embedding.TextChunk,
                RelevanceScore = x.score,
                FilePath = x.embedding.Document.FilePath,
                StartIndex = 0, // Could be calculated if needed
                EndIndex = x.embedding.TextChunk.Length
            })
            .ToList();
    }

    public async Task DeleteDocumentAsync(string filePath)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.FilePath == filePath);

        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateDocumentAsync(string filePath)
    {
        await ProcessDocumentAsync(filePath);
    }

    public async Task<List<DocumentEntity>> GetAllDocumentsAsync()
    {
        return await _context.Documents
            .Include(d => d.Embeddings)
            .ToListAsync();
    }

    public async Task<bool> IsDocumentProcessedAsync(string filePath)
    {
        return await _context.Documents
            .AnyAsync(d => d.FilePath == filePath);
    }

    public async Task<DateTime?> GetDocumentLastProcessedAsync(string filePath)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.FilePath == filePath);

        return document?.LastModified;
    }

    private List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        var sentences = SplitIntoSentences(text);

        var currentChunk = new StringBuilder();
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            currentChunk.Append(sentence).Append(" ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var currentSentence = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            currentSentence.Append(currentChar);

            if (currentChar == '.' || currentChar == '!' || currentChar == '?')
            {
                if (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1]) || text[i + 1] == '\n')
                {
                    var sentence = currentSentence.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(sentence))
                    {
                        sentences.Add(sentence);
                    }
                    currentSentence.Clear();
                }
            }
        }

        var lastSentence = currentSentence.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastSentence))
        {
            sentences.Add(lastSentence);
        }

        return sentences;
    }

    private static byte[] ConvertFloatArrayToBytes(float[] array)
    {
        var bytes = new byte[array.Length * sizeof(float)];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] ConvertBytesToFloatArray(byte[] bytes, int dimensions)
    {
        var array = new float[dimensions];
        Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
        return array;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _fileWatcher?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }
}
