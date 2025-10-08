using AskMeNow.Core.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;

namespace AskMeNow.Infrastructure.Services
{
    public class EmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _modelVersion = "1.0";
        private readonly int _vectorDimensions = 384;
        private bool _disposed = false;

        public EmbeddingService()
        {
            _session = null!;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new float[_vectorDimensions];

            return await Task.Run(() => GenerateSimpleEmbedding(text));
        }

        private float[] GenerateSimpleEmbedding(string text)
        {
            var embedding = new float[_vectorDimensions];
            var words = text.ToLowerInvariant()
                           .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}' },
                                  StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2)
                           .ToArray();

            for (int i = 0; i < _vectorDimensions; i++)
            {
                float value = 0f;
                foreach (var word in words)
                {
                    var hash = word.GetHashCode();
                    var normalizedHash = (hash % 1000) / 1000f;
                    value += normalizedHash * (float)Math.Sin(i * 0.1 + hash * 0.01);
                }
                embedding[i] = (float)Math.Tanh(value / words.Length);
            }

            var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= (float)magnitude;
                }
            }

            return embedding;
        }

        public async Task<float> CalculateSimilarityAsync(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have the same dimensions");

            return await Task.Run(() =>
            {
                float dotProduct = 0f;
                float magnitude1 = 0f;
                float magnitude2 = 0f;

                for (int i = 0; i < vector1.Length; i++)
                {
                    dotProduct += vector1[i] * vector2[i];
                    magnitude1 += vector1[i] * vector1[i];
                    magnitude2 += vector2[i] * vector2[i];
                }

                magnitude1 = (float)Math.Sqrt(magnitude1);
                magnitude2 = (float)Math.Sqrt(magnitude2);

                if (magnitude1 == 0 || magnitude2 == 0)
                    return 0f;

                return dotProduct / (magnitude1 * magnitude2);
            });
        }

        public int GetVectorDimensions() => _vectorDimensions;

        public string GetModelVersion() => _modelVersion;

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }
}
