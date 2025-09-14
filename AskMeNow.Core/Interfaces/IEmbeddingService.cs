namespace AskMeNow.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<float> CalculateSimilarityAsync(float[] vector1, float[] vector2);
    int GetVectorDimensions();
    string GetModelVersion();
}
