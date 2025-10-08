using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;

namespace AskMeNow.Infrastructure.Services
{
    public class PersistentKnowledgeBaseService : IPersistentKnowledgeBaseService, IDisposable
    {
        private readonly KnowledgeBaseContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly IDocumentParserService _documentParserService;
        private readonly ISemanticChunkingService _semanticChunkingService;
        private readonly FileSystemWatcher? _fileWatcher;
        private readonly string _watchedFolderPath = string.Empty;
        private bool _disposed = false;

        public PersistentKnowledgeBaseService(
            KnowledgeBaseContext context,
            IEmbeddingService embeddingService,
            IDocumentParserService documentParserService,
            ISemanticChunkingService semanticChunkingService)
        {
            _context = context;
            _embeddingService = embeddingService;
            _documentParserService = documentParserService;
            _semanticChunkingService = semanticChunkingService;
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

            if (existingDocument != null && existingDocument.LastModified >= fileInfo.LastWriteTime)
            {
                return;
            }


            string content;
            try
            {
                var document = await _documentParserService.ParseDocumentAsync(filePath);
                content = document?.Content ?? "No content extracted";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse document {filePath}: {ex.Message}");
                content = "No content extracted - parsing failed";
            }

            if (existingDocument != null)
            {
                _context.Embeddings.RemoveRange(existingDocument.Embeddings);
                _context.Documents.Remove(existingDocument);
            }

            var documentEntity = new DocumentEntity
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                LastModified = fileInfo.LastWriteTime,
                Language = "en",
                FileType = Path.GetExtension(filePath).ToLowerInvariant(),
                FileSizeBytes = fileInfo.Length
            };

            _context.Documents.Add(documentEntity);
            await _context.SaveChangesAsync();

            var semanticChunks = await _semanticChunkingService.ChunkDocumentAsync(content, documentEntity.FileName, documentEntity.FilePath);
            var embeddings = new List<EmbeddingEntity>();

            for (int i = 0; i < semanticChunks.Count; i++)
            {
                var chunk = semanticChunks[i];
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);

                var embeddingEntity = new EmbeddingEntity
                {
                    DocumentId = documentEntity.Id,
                    ChunkIndex = i,
                    TextChunk = chunk.Content,
                    Vector = ConvertFloatArrayToBytes(embedding),
                    VectorDimensions = embedding.Length,
                    ModelVersion = _embeddingService.GetModelVersion()
                };

                embeddings.Add(embeddingEntity);
            }

            _context.Embeddings.AddRange(embeddings);
            await _context.SaveChangesAsync();

            await ValidateDocumentEmbeddingsAsync(documentEntity.Id, documentEntity.FileName);
        }

        private async Task ValidateDocumentEmbeddingsAsync(int documentId, string fileName)
        {
            try
            {
                var testQuery = Path.GetFileNameWithoutExtension(fileName);
                var testEmbedding = await _embeddingService.GenerateEmbeddingAsync(testQuery);

                var documentEmbeddings = await _context.Embeddings
                    .Where(e => e.DocumentId == documentId)
                    .ToListAsync();

                var maxSimilarity = 0f;
                foreach (var embedding in documentEmbeddings)
                {
                    var storedVector = ConvertBytesToFloatArray(embedding.Vector, embedding.VectorDimensions);
                    var similarity = await _embeddingService.CalculateSimilarityAsync(testEmbedding, storedVector);
                    maxSimilarity = Math.Max(maxSimilarity, similarity);
                }

                if (maxSimilarity < 0.1f)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Low embedding similarity for document '{fileName}'. Max similarity: {maxSimilarity:F3}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Embedding validation successful for '{fileName}'. Max similarity: {maxSimilarity:F3}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating embeddings for '{fileName}': {ex.Message}");
            }
        }

        public async Task ProcessDocumentsInFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            await ClearAllDocumentsAsync();

            var supportedExtensions = _documentParserService.GetSupportedExtensions();
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            var supportedFiles = allFiles
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            foreach (var file in supportedFiles)
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

            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

            var allEmbeddings = await _context.Embeddings
                .Include(e => e.Document)
                .ToListAsync();

            var scoredSnippets = new List<(EmbeddingEntity embedding, float score)>();

            foreach (var embedding in allEmbeddings)
            {
                var storedVector = ConvertBytesToFloatArray(embedding.Vector, embedding.VectorDimensions);
                var similarity = await _embeddingService.CalculateSimilarityAsync(questionEmbedding, storedVector);

                if (similarity > 0.1f)
                {
                    scoredSnippets.Add((embedding, similarity));
                }
            }

            return scoredSnippets
                .OrderByDescending(x => x.score)
                .Take(maxSnippets)
                .Select(x => new DocumentSnippet
                {
                    FileName = x.embedding.Document.FileName,
                    SnippetText = x.embedding.TextChunk,
                    RelevanceScore = x.score,
                    FilePath = x.embedding.Document.FilePath,
                    StartIndex = 0,
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

        private async Task ClearAllDocumentsAsync()
        {
            var allEmbeddings = await _context.Embeddings.ToListAsync();
            _context.Embeddings.RemoveRange(allEmbeddings);

            var allDocuments = await _context.Documents.ToListAsync();
            _context.Documents.RemoveRange(allDocuments);

            await _context.SaveChangesAsync();
        }

        public async Task<EmbeddingValidationResult> ValidateAllEmbeddingsAsync()
        {
            var result = new EmbeddingValidationResult();

            try
            {
                result.TotalEmbeddings = await _context.Embeddings.CountAsync();

                if (result.TotalEmbeddings == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("No embeddings found in database");
                    return result;
                }

                var testQueries = new List<string>
            {
                "introduction",
                "summary",
                "overview",
                "main topic",
                "key points",
                "conclusion"
            };

                var allEmbeddings = await _context.Embeddings
                    .Include(e => e.Document)
                    .Take(200)
                    .ToListAsync();

                result.TestedEmbeddings = allEmbeddings.Count;
                var totalSimilarity = 0f;
                var validTests = 0;

                foreach (var testQuery in testQueries)
                {
                    try
                    {
                        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(testQuery);
                        var similarities = new List<float>();

                        foreach (var embedding in allEmbeddings)
                        {
                            var storedVector = ConvertBytesToFloatArray(embedding.Vector, embedding.VectorDimensions);
                            var similarity = await _embeddingService.CalculateSimilarityAsync(queryEmbedding, storedVector);
                            similarities.Add(similarity);
                        }

                        var maxSimilarity = similarities.Max();
                        var avgSimilarity = similarities.Average();

                        result.TestResults.Add($"Query '{testQuery}': Max similarity = {maxSimilarity:F3}, Avg similarity = {avgSimilarity:F3}");

                        totalSimilarity += maxSimilarity;
                        validTests++;

                        if (maxSimilarity < 0.1f)
                        {
                            result.Errors.Add($"No good matches found for test query: '{testQuery}' (max similarity: {maxSimilarity:F3})");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error testing query '{testQuery}': {ex.Message}");
                    }
                }

                result.AverageSimilarity = validTests > 0 ? totalSimilarity / validTests : 0f;
                result.IsValid = result.Errors.Count == 0 && result.AverageSimilarity > 0.1f;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed: {ex.Message}");
            }

            return result;
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
}