using AskMeNow.Core.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services
{
    public class SemanticChunkingService : ISemanticChunkingService
    {
        private const int TargetChunkTokens = 600;
        private const int MaxChunkTokens = 800;
        private const int MinChunkTokens = 200;
        private const int OverlapTokens = 75;
        private const int TokensPerCharacter = 4;

        public async Task<List<SemanticChunk>> ChunkDocumentAsync(string content, string fileName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<SemanticChunk>();

            var segments = SplitIntoSemanticSegments(content);
            var chunks = new List<SemanticChunk>();
            var currentChunk = new StringBuilder();
            var currentHeaders = new List<string>();
            var chunkIndex = 0;
            var currentTokenCount = 0;

            foreach (var segment in segments)
            {
                var segmentTokens = EstimateTokenCount(segment.Content);

                if (currentTokenCount + segmentTokens > MaxChunkTokens && currentChunk.Length > 0)
                {
                    var chunk = CreateChunk(currentChunk.ToString(), fileName, filePath, chunkIndex, currentHeaders);
                    chunks.Add(chunk);

                    var overlapText = GetOverlapText(currentChunk.ToString(), OverlapTokens);
                    currentChunk.Clear();
                    currentChunk.Append(overlapText);
                    currentTokenCount = EstimateTokenCount(overlapText);
                    chunkIndex++;
                }

                if (segment.Type == SegmentType.Heading)
                {
                    currentHeaders.Add(segment.Content.Trim());
                    if (currentHeaders.Count > 3)
                        currentHeaders.RemoveAt(0);
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append("\n\n");

                currentChunk.Append(segment.Content);
                currentTokenCount += segmentTokens;

                if (segmentTokens > MaxChunkTokens)
                {
                    var subChunks = SplitLargeSegment(segment, fileName, filePath, chunkIndex, currentHeaders);
                    chunks.AddRange(subChunks);
                    chunkIndex += subChunks.Count;

                    currentChunk.Clear();
                    currentTokenCount = 0;
                }
            }

            if (currentChunk.Length > 0)
            {
                var chunk = CreateChunk(currentChunk.ToString(), fileName, filePath, chunkIndex, currentHeaders);
                chunks.Add(chunk);
            }

            return await Task.FromResult(chunks);
        }

        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return Math.Max(1, text.Length / TokensPerCharacter);
        }

        public List<SemanticSegment> SplitIntoSemanticSegments(string text)
        {
            var segments = new List<SemanticSegment>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var currentSegment = new StringBuilder();
            var currentType = SegmentType.Paragraph;
            var startIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var segmentType = DetermineSegmentType(line);

                if (currentSegment.Length > 0 && (segmentType != currentType || IsMajorBoundary(segmentType)))
                {
                    var content = currentSegment.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        segments.Add(new SemanticSegment
                        {
                            Content = content,
                            Type = currentType,
                            StartIndex = startIndex,
                            EndIndex = startIndex + content.Length,
                            TokenCount = EstimateTokenCount(content)
                        });
                    }

                    currentSegment.Clear();
                    startIndex += content.Length + 1;
                }

                if (currentSegment.Length > 0)
                    currentSegment.Append('\n');

                currentSegment.Append(line);
                currentType = segmentType;
            }

            var finalContent = currentSegment.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                segments.Add(new SemanticSegment
                {
                    Content = finalContent,
                    Type = currentType,
                    StartIndex = startIndex,
                    EndIndex = startIndex + finalContent.Length,
                    TokenCount = EstimateTokenCount(finalContent)
                });
            }

            return segments;
        }

        private SegmentType DetermineSegmentType(string line)
        {
            if (line.StartsWith("#") || line.StartsWith("##") || line.StartsWith("###"))
                return SegmentType.Heading;

            if (line.Length < 100 && line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c)))
                return SegmentType.Heading;

            if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• ") ||
                Regex.IsMatch(line, @"^\d+\.\s") || Regex.IsMatch(line, @"^\d+\)\s"))
                return SegmentType.List;

            if (line.StartsWith("```") || line.StartsWith("    ") || line.StartsWith("\t"))
                return SegmentType.Code;

            if (line.Count(c => c == '|') >= 2)
                return SegmentType.Table;

            return SegmentType.Paragraph;
        }

        private bool IsMajorBoundary(SegmentType type)
        {
            return type == SegmentType.Heading || type == SegmentType.Code;
        }

        private SemanticChunk CreateChunk(string content, string fileName, string filePath, int chunkIndex, List<string> headers)
        {
            var chunkType = DetermineChunkType(content);

            return new SemanticChunk
            {
                Content = content.Trim(),
                SourceDocument = fileName,
                FilePath = filePath,
                ChunkIndex = chunkIndex,
                TokenCount = EstimateTokenCount(content),
                Type = chunkType,
                Headers = new List<string>(headers)
            };
        }

        private ChunkType DetermineChunkType(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Any(l => l.StartsWith("#")))
                return ChunkType.Heading;

            if (lines.Any(l => l.StartsWith("- ") || l.StartsWith("* ") || Regex.IsMatch(l, @"^\d+\.\s")))
                return ChunkType.List;

            if (lines.Any(l => l.Count(c => c == '|') >= 2))
                return ChunkType.Table;

            if (lines.Any(l => l.StartsWith("```") || l.StartsWith("    ")))
                return ChunkType.Code;

            var types = lines.Select(DetermineSegmentType).Distinct().ToList();
            if (types.Count > 1)
                return ChunkType.Mixed;

            return ChunkType.Paragraph;
        }

        private List<SemanticChunk> SplitLargeSegment(SemanticSegment segment, string fileName, string filePath, int startChunkIndex, List<string> headers)
        {
            var chunks = new List<SemanticChunk>();
            var sentences = SplitIntoSentences(segment.Content);
            var currentChunk = new StringBuilder();
            var currentTokenCount = 0;
            var chunkIndex = startChunkIndex;

            foreach (var sentence in sentences)
            {
                var sentenceTokens = EstimateTokenCount(sentence);

                if (currentTokenCount + sentenceTokens > MaxChunkTokens && currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(currentChunk.ToString(), fileName, filePath, chunkIndex, headers));
                    currentChunk.Clear();
                    currentTokenCount = 0;
                    chunkIndex++;
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append(" ");

                currentChunk.Append(sentence);
                currentTokenCount += sentenceTokens;
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(currentChunk.ToString(), fileName, filePath, chunkIndex, headers));
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

        private string GetOverlapText(string text, int overlapTokens)
        {
            var targetChars = overlapTokens * TokensPerCharacter;
            if (text.Length <= targetChars)
                return text;

            var sentences = SplitIntoSentences(text);
            var result = new StringBuilder();

            foreach (var sentence in sentences)
            {
                if (result.Length + sentence.Length > targetChars)
                    break;

                if (result.Length > 0)
                    result.Append(" ");

                result.Append(sentence);
            }

            return result.ToString();
        }
    }
}