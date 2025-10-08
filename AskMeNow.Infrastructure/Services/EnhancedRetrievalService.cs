using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using AskMeNow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services
{
    public class EnhancedRetrievalService : IEnhancedRetrievalService
    {
        private readonly KnowledgeBaseContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBedrockClientService _bedrockService;

        public EnhancedRetrievalService(
            KnowledgeBaseContext context,
            IEmbeddingService embeddingService,
            IBedrockClientService bedrockService)
        {
            _context = context;
            _embeddingService = embeddingService;
            _bedrockService = bedrockService;
        }

        public async Task<List<RetrievalResult>> RetrieveRelevantChunksAsync(string query, int maxChunks = 10, float minSimilarityThreshold = 0.1f)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<RetrievalResult>();

            var expandedQueries = await GenerateExpandedQueriesAsync(query);
            var allResults = new List<RetrievalResult>();

            foreach (var expandedQuery in expandedQueries)
            {
                var queryResults = await RetrieveForSingleQueryAsync(expandedQuery.Query, maxChunks, minSimilarityThreshold);

                foreach (var result in queryResults)
                {
                    result.SourceQuery = expandedQuery.Query;
                    result.IsFromExpandedQuery = expandedQuery.Type != QueryType.Original;
                    result.SimilarityScore *= expandedQuery.Weight;
                }

                allResults.AddRange(queryResults);
            }

            var mergedResults = MergeRetrievalResults(allResults);

            return mergedResults
                .OrderByDescending(r => r.SimilarityScore)
                .Take(maxChunks)
                .ToList();
        }

        public async Task<List<ExpandedQuery>> GenerateExpandedQueriesAsync(string originalQuery)
        {
            var expandedQueries = new List<ExpandedQuery>
        {
            new ExpandedQuery
            {
                Query = originalQuery,
                Type = QueryType.Original,
                Weight = 1.0f,
                Reason = "Original query"
            }
        };

            if (IsVagueQuery(originalQuery))
            {
                var broaderQueries = await GenerateBroaderQueriesAsync(originalQuery);
                var narrowerQueries = await GenerateNarrowerQueriesAsync(originalQuery);

                expandedQueries.AddRange(broaderQueries);
                expandedQueries.AddRange(narrowerQueries);
            }

            var synonymQueries = await GenerateSynonymQueriesAsync(originalQuery);
            expandedQueries.AddRange(synonymQueries);

            var contextualQueries = await GenerateContextualQueriesAsync(originalQuery);
            expandedQueries.AddRange(contextualQueries);

            return expandedQueries;
        }

        public async Task<EmbeddingValidationResult> ValidateEmbeddingsAsync(List<string>? testQueries = null)
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

                testQueries ??= new List<string>
            {
                "introduction",
                "summary",
                "overview",
                "main topic",
                "key points"
            };

                var allEmbeddings = await _context.Embeddings
                    .Include(e => e.Document)
                    .Take(100)
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

        public async Task<List<FallbackSuggestion>> GetFallbackSuggestionsAsync(string query, int maxSuggestions = 5)
        {
            var suggestions = new List<FallbackSuggestion>();

            try
            {
                var documents = await _context.Documents
                    .Include(d => d.Embeddings)
                    .ToListAsync();

                if (!documents.Any())
                {
                    return suggestions;
                }

                var topics = new Dictionary<string, int>();

                foreach (var doc in documents)
                {
                    var titleWords = ExtractKeywords(doc.FileName);
                    foreach (var word in titleWords)
                    {
                        topics[word] = topics.GetValueOrDefault(word, 0) + 1;
                    }

                    var sampleChunks = doc.Embeddings.Take(3).ToList();
                    foreach (var chunk in sampleChunks)
                    {
                        var chunkWords = ExtractKeywords(chunk.TextChunk);
                        foreach (var word in chunkWords.Take(10))
                        {
                            topics[word] = topics.GetValueOrDefault(word, 0) + 1;
                        }
                    }
                }

                var sortedTopics = topics
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(maxSuggestions)
                    .ToList();

                foreach (var topic in sortedTopics)
                {
                    var relatedDocs = documents
                        .Where(d => d.FileName.Contains(topic.Key, StringComparison.OrdinalIgnoreCase) ||
                                   d.Embeddings.Any(e => e.TextChunk.Contains(topic.Key, StringComparison.OrdinalIgnoreCase)))
                        .Take(3)
                        .ToList();

                    suggestions.Add(new FallbackSuggestion
                    {
                        Topic = topic.Key,
                        Description = $"Information about {topic.Key}",
                        SourceDocument = relatedDocs.FirstOrDefault()?.FileName ?? "Multiple documents",
                        RelevanceScore = topic.Value / (float)documents.Count,
                        RelatedChunks = relatedDocs
                            .SelectMany(d => d.Embeddings.Take(2))
                            .Select(e => e.TextChunk.Substring(0, Math.Min(100, e.TextChunk.Length)) + "...")
                            .ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating fallback suggestions: {ex.Message}");
            }

            return suggestions;
        }

        private async Task<List<RetrievalResult>> RetrieveForSingleQueryAsync(string query, int maxChunks, float minSimilarityThreshold)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            var allEmbeddings = await _context.Embeddings
                .Include(e => e.Document)
                .ToListAsync();

            var results = new List<RetrievalResult>();

            foreach (var embedding in allEmbeddings)
            {
                var storedVector = ConvertBytesToFloatArray(embedding.Vector, embedding.VectorDimensions);
                var similarity = await _embeddingService.CalculateSimilarityAsync(queryEmbedding, storedVector);

                if (similarity >= minSimilarityThreshold)
                {
                    var chunk = new SemanticChunk
                    {
                        Id = embedding.Id.ToString(),
                        Content = embedding.TextChunk,
                        SourceDocument = embedding.Document.FileName,
                        FilePath = embedding.Document.FilePath,
                        ChunkIndex = embedding.ChunkIndex,
                        TokenCount = EstimateTokenCount(embedding.TextChunk)
                    };

                    results.Add(new RetrievalResult
                    {
                        Chunk = chunk,
                        SimilarityScore = similarity,
                        SourceQuery = query
                    });
                }
            }

            return results.OrderByDescending(r => r.SimilarityScore).Take(maxChunks).ToList();
        }

        private List<RetrievalResult> MergeRetrievalResults(List<RetrievalResult> results)
        {
            var merged = new Dictionary<string, RetrievalResult>();

            foreach (var result in results)
            {
                var key = result.Chunk.Id;

                if (merged.ContainsKey(key))
                {
                    if (result.SimilarityScore > merged[key].SimilarityScore)
                    {
                        merged[key] = result;
                    }
                }
                else
                {
                    merged[key] = result;
                }
            }

            return merged.Values.ToList();
        }

        private bool IsVagueQuery(string query)
        {
            var vaguePatterns = new[]
            {
            @"\b(what|how|why|when|where|who)\b.*\b(about|is|are|do|does|did)\b",
            @"\b(tell me|explain|describe|overview|summary)\b",
            @"\b(everything|all|general|basic|main)\b"
        };

            return vaguePatterns.Any(pattern => Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase));
        }

        private async Task<List<ExpandedQuery>> GenerateBroaderQueriesAsync(string originalQuery)
        {
            var broaderQueries = new List<ExpandedQuery>();

            if (originalQuery.Contains("how to", StringComparison.OrdinalIgnoreCase))
            {
                broaderQueries.Add(new ExpandedQuery
                {
                    Query = originalQuery.Replace("how to", "information about", StringComparison.OrdinalIgnoreCase),
                    Type = QueryType.Broader,
                    Weight = 0.8f,
                    Reason = "Broader context for how-to question"
                });
            }

            if (originalQuery.Contains("what is", StringComparison.OrdinalIgnoreCase))
            {
                broaderQueries.Add(new ExpandedQuery
                {
                    Query = originalQuery.Replace("what is", "about", StringComparison.OrdinalIgnoreCase),
                    Type = QueryType.Broader,
                    Weight = 0.8f,
                    Reason = "Broader context for definition question"
                });
            }

            return broaderQueries;
        }

        private async Task<List<ExpandedQuery>> GenerateNarrowerQueriesAsync(string originalQuery)
        {
            var narrowerQueries = new List<ExpandedQuery>();

            var keywords = ExtractKeywords(originalQuery);

            foreach (var keyword in keywords.Take(3))
            {
                narrowerQueries.Add(new ExpandedQuery
                {
                    Query = keyword,
                    Type = QueryType.Narrower,
                    Weight = 0.6f,
                    Reason = $"Specific focus on key term: {keyword}"
                });
            }

            return narrowerQueries;
        }

        private async Task<List<ExpandedQuery>> GenerateSynonymQueriesAsync(string originalQuery)
        {
            var synonymQueries = new List<ExpandedQuery>();

            var synonyms = new Dictionary<string, string[]>
            {
                ["help"] = new[] { "assist", "support", "aid" },
                ["problem"] = new[] { "issue", "trouble", "difficulty" },
                ["information"] = new[] { "data", "details", "facts" },
                ["explain"] = new[] { "describe", "clarify", "elaborate" }
            };

            foreach (var synonym in synonyms)
            {
                if (originalQuery.Contains(synonym.Key, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var replacement in synonym.Value)
                    {
                        var synonymQuery = originalQuery.Replace(synonym.Key, replacement, StringComparison.OrdinalIgnoreCase);
                        synonymQueries.Add(new ExpandedQuery
                        {
                            Query = synonymQuery,
                            Type = QueryType.Synonym,
                            Weight = 0.7f,
                            Reason = $"Synonym replacement: {synonym.Key} -> {replacement}"
                        });
                    }
                }
            }

            return synonymQueries;
        }

        private async Task<List<ExpandedQuery>> GenerateContextualQueriesAsync(string originalQuery)
        {
            var contextualQueries = new List<ExpandedQuery>();

            var contextualTerms = new[] { "introduction", "overview", "basics", "fundamentals" };

            foreach (var term in contextualTerms)
            {
                contextualQueries.Add(new ExpandedQuery
                {
                    Query = $"{originalQuery} {term}",
                    Type = QueryType.Contextual,
                    Weight = 0.5f,
                    Reason = $"Contextual expansion with: {term}"
                });
            }

            return contextualQueries;
        }

        private List<string> ExtractKeywords(string text)
        {
            var commonWords = new HashSet<string>
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did",
            "will", "would", "could", "should", "may", "might", "can", "what", "how", "when", "where", "why", "who",
            "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they"
        };

            return text.Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' },
                             StringSplitOptions.RemoveEmptyEntries)
                       .Where(word => word.Length > 2 && !commonWords.Contains(word.ToLowerInvariant()))
                       .Select(word => word.ToLowerInvariant())
                       .ToList();
        }

        private int EstimateTokenCount(string text)
        {
            return Math.Max(1, text.Length / 4);
        }

        private static float[] ConvertBytesToFloatArray(byte[] bytes, int dimensions)
        {
            var array = new float[dimensions];
            Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            return array;
        }
    }

}