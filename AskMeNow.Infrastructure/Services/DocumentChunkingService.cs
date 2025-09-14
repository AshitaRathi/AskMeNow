using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Text;

namespace AskMeNow.Infrastructure.Services;

public class DocumentChunkingService : IDocumentChunkingService
{
    private const int MaxChunkSize = 1000; // Maximum characters per chunk
    private const int OverlapSize = 200; // Overlap between chunks

    public List<DocumentChunk> ChunkDocuments(List<FAQDocument> documents)
    {
        var chunks = new List<DocumentChunk>();

        foreach (var document in documents)
        {
            var documentChunks = ChunkDocument(document);
            chunks.AddRange(documentChunks);
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkDocument(FAQDocument document)
    {
        var chunks = new List<DocumentChunk>();
        var content = document.Content;
        var startIndex = 0;
        var chunkIndex = 0;

        while (startIndex < content.Length)
        {
            var endIndex = Math.Min(startIndex + MaxChunkSize, content.Length);
            
            // Try to break at a sentence or paragraph boundary
            if (endIndex < content.Length)
            {
                var lastPeriod = content.LastIndexOf('.', endIndex - 1, MaxChunkSize);
                var lastNewline = content.LastIndexOf('\n', endIndex - 1, MaxChunkSize);
                var lastSpace = content.LastIndexOf(' ', endIndex - 1, MaxChunkSize);
                
                var breakPoint = Math.Max(lastPeriod, Math.Max(lastNewline, lastSpace));
                if (breakPoint > startIndex + MaxChunkSize / 2) // Don't make chunks too small
                {
                    endIndex = breakPoint + 1;
                }
            }

            var chunkContent = content.Substring(startIndex, endIndex - startIndex).Trim();
            
            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new DocumentChunk
                {
                    Content = chunkContent,
                    SourceDocument = document.Title,
                    ChunkIndex = chunkIndex
                });
                chunkIndex++;
            }

            // Move start index with overlap
            startIndex = Math.Max(startIndex + 1, endIndex - OverlapSize);
        }

        return chunks;
    }

    public List<DocumentChunk> FindRelevantChunks(string question, List<DocumentChunk> chunks, int maxChunks = 5)
    {
        if (string.IsNullOrWhiteSpace(question) || !chunks.Any())
            return new List<DocumentChunk>();

        var questionWords = ExtractKeywords(question.ToLower());
        var scoredChunks = new List<(DocumentChunk chunk, double score)>();

        foreach (var chunk in chunks)
        {
            var score = CalculateRelevanceScore(questionWords, chunk.Content.ToLower());
            scoredChunks.Add((chunk, score));
        }

        return scoredChunks
            .OrderByDescending(x => x.score)
            .Take(maxChunks)
            .Where(x => x.score > 0) // Only return chunks with some relevance
            .Select(x => 
            {
                x.chunk.RelevanceScore = x.score;
                return x.chunk;
            })
            .ToList();
    }

    private List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction - remove common words and split
        var commonWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "what", "how", "when", "where", "why", "who" };
        
        return text.Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries)
                   .Where(word => word.Length > 2 && !commonWords.Contains(word))
                   .ToList();
    }

    private double CalculateRelevanceScore(List<string> questionWords, string chunkContent)
    {
        if (!questionWords.Any())
            return 0;

        var chunkWords = ExtractKeywords(chunkContent);
        var matches = questionWords.Count(word => chunkWords.Contains(word));
        
        // Calculate score based on word matches and chunk length
        var baseScore = (double)matches / questionWords.Count;
        var lengthPenalty = Math.Min(1.0, chunkWords.Count / 50.0); // Prefer chunks with reasonable length
        
        return baseScore * lengthPenalty;
    }

    public List<DocumentSnippet> GetRelevantSnippets(string question, List<FAQDocument> documents, int maxSnippets = 5)
    {
        if (string.IsNullOrWhiteSpace(question) || !documents.Any())
            return new List<DocumentSnippet>();

        var questionWords = ExtractKeywords(question.ToLower());
        var snippets = new List<DocumentSnippet>();

        foreach (var document in documents)
        {
            var documentSnippets = ExtractRelevantSnippetsFromDocument(question, questionWords, document);
            snippets.AddRange(documentSnippets);
        }

        return snippets
            .OrderByDescending(s => s.RelevanceScore)
            .Take(maxSnippets)
            .Where(s => s.RelevanceScore > 0)
            .ToList();
    }

    private List<DocumentSnippet> ExtractRelevantSnippetsFromDocument(string question, List<string> questionWords, FAQDocument document)
    {
        var snippets = new List<DocumentSnippet>();
        var content = document.Content;
        var sentences = SplitIntoSentences(content);

        for (int i = 0; i < sentences.Count; i++)
        {
            var sentence = sentences[i];
            var score = CalculateRelevanceScore(questionWords, sentence.ToLower());
            
            if (score > 0)
            {
                // Create a snippet with context (previous + current + next sentence)
                var startIndex = Math.Max(0, i - 1);
                var endIndex = Math.Min(sentences.Count - 1, i + 1);
                
                var snippetText = string.Join(" ", sentences.Skip(startIndex).Take(endIndex - startIndex + 1));
                var highlightedSentences = new List<string> { sentence };

                // Find the position in the original document
                var sentenceStart = content.IndexOf(sentence);
                var snippetStart = content.IndexOf(snippetText);

                snippets.Add(new DocumentSnippet
                {
                    FileName = Path.GetFileName(document.FilePath),
                    SnippetText = snippetText,
                    StartIndex = snippetStart >= 0 ? snippetStart : 0,
                    EndIndex = snippetStart >= 0 ? snippetStart + snippetText.Length : snippetText.Length,
                    RelevanceScore = score,
                    HighlightedSentences = highlightedSentences,
                    FilePath = document.FilePath
                });
            }
        }

        return snippets;
    }

    private List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting - can be enhanced with more sophisticated NLP
        var sentences = new List<string>();
        var currentSentence = new StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            currentSentence.Append(currentChar);
            
            // Check for sentence endings
            if (currentChar == '.' || currentChar == '!' || currentChar == '?')
            {
                // Look ahead to see if it's really the end of a sentence
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
        
        // Add the last sentence if there's any remaining text
        var lastSentence = currentSentence.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastSentence))
        {
            sentences.Add(lastSentence);
        }
        
        return sentences;
    }
}
