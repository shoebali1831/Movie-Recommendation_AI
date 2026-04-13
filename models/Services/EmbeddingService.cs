using OpenAI.Embeddings;

namespace MovieApi.Services;

public sealed class EmbeddingService
{
    private readonly EmbeddingClient _client;

    public EmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var result = await _client.GenerateEmbeddingAsync(text);
        return result.Value.ToFloats().ToArray();
    }
}
