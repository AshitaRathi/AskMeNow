using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services
{
    public class DocumentPreviewService : IDocumentPreviewService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IBedrockClientService _bedrockService;
        private readonly IDocumentParserService _documentParserService;
        private readonly Dictionary<string, List<DocumentHighlight>> _highlightsCache = new();
        private readonly Dictionary<string, int> _referenceCounts = new();
        private readonly Dictionary<string, string> _documentContentCache = new();
        private readonly Dictionary<string, string> _summaryCache = new();

        public DocumentPreviewService(IDocumentRepository documentRepository, IBedrockClientService bedrockService, IDocumentParserService documentParserService)
        {
            _documentRepository = documentRepository;
            _bedrockService = bedrockService;
            _documentParserService = documentParserService;
        }

        public async Task<DocumentPreview?> GetDocumentPreviewAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var content = await GetDocumentContentAsync(filePath);
                if (string.IsNullOrEmpty(content) || content == "Unable to extract content from this file type.")
                    return null;

                var summary = await GenerateSummaryAsync(filePath, content);
                var highlights = await GetHighlightsForDocumentAsync(filePath);
                var referencedSnippets = await GetReferencedSnippetsAsync(filePath);

                var fileInfo = new FileInfo(filePath);

                return new DocumentPreview
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    Content = content,
                    Summary = summary,
                    FileExtension = fileInfo.Extension.ToLowerInvariant(),
                    LastModified = fileInfo.LastWriteTime,
                    WordCount = CountWords(content),
                    FileSizeBytes = fileInfo.Length,
                    Highlights = highlights,
                    ReferencedSnippets = referencedSnippets
                };
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<DocumentPreview>> GetAllDocumentPreviewsAsync()
        {
            try
            {
                var allContent = _documentRepository.GetAllContent();
                if (string.IsNullOrEmpty(allContent))
                    return new List<DocumentPreview>();

                return new List<DocumentPreview>();
            }
            catch (Exception)
            {
                return new List<DocumentPreview>();
            }
        }

        public async Task<List<DocumentHighlight>> GetHighlightsForDocumentAsync(string filePath)
        {
            if (_highlightsCache.TryGetValue(filePath, out var cachedHighlights))
            {
                return cachedHighlights;
            }

            try
            {
                var content = await GetDocumentContentAsync(filePath);
                if (string.IsNullOrEmpty(content))
                    return new List<DocumentHighlight>();

                var highlights = await GenerateHighlightsAsync(filePath, content);
                _highlightsCache[filePath] = highlights;
                return highlights;
            }
            catch (Exception)
            {
                return new List<DocumentHighlight>();
            }
        }

        public async Task UpdateHighlightsAsync(string filePath, List<DocumentSnippet> referencedSnippets)
        {
            try
            {
                var content = await GetDocumentContentAsync(filePath);
                if (string.IsNullOrEmpty(content))
                    return;

                foreach (var snippet in referencedSnippets)
                {
                    var snippetText = snippet.SnippetText.Trim();
                    if (!string.IsNullOrEmpty(snippetText))
                    {
                        var key = $"{filePath}:{snippetText}";
                        _referenceCounts[key] = _referenceCounts.GetValueOrDefault(key, 0) + 1;
                    }
                }

                var highlights = await GenerateHighlightsAsync(filePath, content);
                _highlightsCache[filePath] = highlights;
            }
            catch (Exception)
            {
            }
        }

        private async Task<string> GetDocumentContentAsync(string filePath)
        {
            if (_documentContentCache.TryGetValue(filePath, out var cachedContent))
            {
                return cachedContent;
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var document = await _documentParserService.ParseDocumentAsync(filePath);
                var content = document?.Content ?? "No content extracted";

                _documentContentCache[filePath] = content;
                return content;
            }
            catch (Exception)
            {
                try
                {
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (extension == ".txt" || extension == ".md")
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        _documentContentCache[filePath] = content;
                        return content;
                    }
                }
                catch (Exception)
                {
                }

                return "Unable to extract content from this file type.";
            }
        }

        private async Task<List<DocumentHighlight>> GenerateHighlightsAsync(string filePath, string content)
        {
            var highlights = new List<DocumentHighlight>();
            var sentences = SplitIntoSentences(content);

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (string.IsNullOrEmpty(trimmedSentence) || trimmedSentence.Length < 20)
                    continue;

                var key = $"{filePath}:{trimmedSentence}";
                var referenceCount = _referenceCounts.GetValueOrDefault(key, 0);

                if (referenceCount > 0)
                {
                    var startIndex = content.IndexOf(trimmedSentence, StringComparison.OrdinalIgnoreCase);
                    if (startIndex >= 0)
                    {
                        var highlight = new DocumentHighlight
                        {
                            StartIndex = startIndex,
                            EndIndex = startIndex + trimmedSentence.Length,
                            Text = trimmedSentence,
                            Type = DetermineHighlightType(referenceCount, trimmedSentence),
                            RelevanceScore = CalculateRelevanceScore(referenceCount, trimmedSentence),
                            ReferenceCount = referenceCount,
                            LastReferenced = DateTime.UtcNow,
                            Tooltip = $"Referenced {referenceCount} time{(referenceCount == 1 ? "" : "s")} in recent answers"
                        };

                        highlights.Add(highlight);
                    }
                }
            }

            var keyConceptHighlights = await GenerateKeyConceptHighlightsAsync(content);
            highlights.AddRange(keyConceptHighlights);

            return highlights
                .OrderByDescending(h => h.ReferenceCount)
                .ThenByDescending(h => h.RelevanceScore)
                .Take(50)
                .ToList();
        }

        private async Task<List<DocumentHighlight>> GenerateKeyConceptHighlightsAsync(string content)
        {
            var highlights = new List<DocumentHighlight>();

            var keyConceptPatterns = new[]
            {
            @"\b(?:important|critical|essential|key|main|primary|significant|major)\b",
            @"\b(?:policy|procedure|guideline|requirement|standard|rule|regulation)\b",
            @"\b(?:deadline|due date|schedule|timeline|milestone)\b",
            @"\b(?:budget|cost|price|fee|payment|expense)\b",
            @"\b(?:contact|email|phone|address|location)\b"
        };

            foreach (var pattern in keyConceptPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var sentenceStart = FindSentenceStart(content, match.Index);
                    var sentenceEnd = FindSentenceEnd(content, match.Index);
                    var sentence = content.Substring(sentenceStart, sentenceEnd - sentenceStart).Trim();

                    if (sentence.Length > 20 && sentence.Length < 200)
                    {
                        var highlight = new DocumentHighlight
                        {
                            StartIndex = sentenceStart,
                            EndIndex = sentenceEnd,
                            Text = sentence,
                            Type = HighlightType.KeyConcept,
                            RelevanceScore = 0.7,
                            ReferenceCount = 0,
                            LastReferenced = DateTime.UtcNow,
                            Tooltip = "Key concept or important information"
                        };

                        highlights.Add(highlight);
                    }
                }
            }

            return highlights.DistinctBy(h => h.Text).ToList();
        }

        private HighlightType DetermineHighlightType(int referenceCount, string text)
        {
            if (referenceCount >= 5)
                return HighlightType.FrequentlyReferenced;
            if (referenceCount >= 2)
                return HighlightType.HighRelevance;
            if (referenceCount >= 1)
                return HighlightType.RecentReference;

            return HighlightType.KeyConcept;
        }

        private double CalculateRelevanceScore(int referenceCount, string text)
        {
            var baseScore = Math.Min(referenceCount * 0.2, 1.0);

            var lengthBonus = Math.Min(text.Length / 200.0, 0.3);

            var specificityBonus = 0.0;
            if (Regex.IsMatch(text, @"\d+") || Regex.IsMatch(text, @"\b(?:january|february|march|april|may|june|july|august|september|october|november|december)\b", RegexOptions.IgnoreCase))
            {
                specificityBonus = 0.2;
            }

            return Math.Min(baseScore + lengthBonus + specificityBonus, 1.0);
        }

        private List<string> SplitIntoSentences(string text)
        {
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            return sentences;
        }

        private int FindSentenceStart(string text, int position)
        {
            var start = position;
            while (start > 0 && !".!?".Contains(text[start - 1]))
            {
                start--;
            }
            return start;
        }

        private int FindSentenceEnd(string text, int position)
        {
            var end = position;
            while (end < text.Length && !".!?".Contains(text[end]))
            {
                end++;
            }
            return end + 1;
        }

        private async Task<List<DocumentSnippet>> GetReferencedSnippetsAsync(string filePath)
        {
            try
            {
                return new List<DocumentSnippet>();
            }
            catch (Exception)
            {
                return new List<DocumentSnippet>();
            }
        }

        private async Task<string> GenerateSummaryAsync(string filePath, string content)
        {
            if (_summaryCache.TryGetValue(filePath, out var cachedSummary))
            {
                return cachedSummary;
            }

            try
            {
                var extractiveSummary = GenerateExtractiveSummary(content);

                if (content.Length < 2000 || extractiveSummary.Length > 50)
                {
                    _summaryCache[filePath] = extractiveSummary;
                    return extractiveSummary;
                }

                var llmSummary = await GenerateLLMSummaryAsync(content);
                if (!string.IsNullOrEmpty(llmSummary))
                {
                    _summaryCache[filePath] = llmSummary;
                    return llmSummary;
                }

                _summaryCache[filePath] = extractiveSummary;
                return extractiveSummary;
            }
            catch (Exception)
            {
                var fallbackSummary = GenerateExtractiveSummary(content);
                _summaryCache[filePath] = fallbackSummary;
                return fallbackSummary;
            }
        }

        private string GenerateExtractiveSummary(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "No content available for summary.";

            var sentences = SplitIntoSentences(content);

            if (sentences.Count == 0)
                return "Document contains no readable text.";

            var summarySentences = sentences.Take(Math.Min(5, sentences.Count)).ToList();

            var meaningfulSentences = summarySentences
                .Where(s => s.Length > 20)
                .Take(3)
                .ToList();

            if (meaningfulSentences.Count == 0)
            {
                meaningfulSentences = summarySentences.Take(2).ToList();
            }

            var summary = string.Join(" ", meaningfulSentences);

            if (summary.Length > 500)
            {
                summary = summary.Substring(0, 497) + "...";
            }

            return string.IsNullOrWhiteSpace(summary) ? "Document summary not available." : summary;
        }

        private async Task<string> GenerateLLMSummaryAsync(string content)
        {
            try
            {
                var truncatedContent = content.Length > 4000 ? content.Substring(0, 4000) + "..." : content;

                var prompt = $@"Please provide a concise summary of the following document in 3-5 sentences. Focus on the main topics, key information, and important details:

{truncatedContent}

Summary:";

                var response = await _bedrockService.GenerateAnswerAsync(prompt, "");

                if (string.IsNullOrWhiteSpace(response))
                    return string.Empty;

                var summary = response.Trim();

                if (summary.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    summary = summary.Substring(8).Trim();
                }

                if (summary.Length > 800)
                {
                    summary = summary.Substring(0, 797) + "...";
                }

                return summary;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}