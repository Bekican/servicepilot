using ServicePilot.Application.Knowledge;

namespace ServicePilot.IntegrationTests.Infrastructure;

internal sealed class FakeTextEmbeddingClient
    : ITextEmbeddingClient
{
    public Task<EmbeddingBatch> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReadOnlyList<float>> embeddings = inputs
            .Select((_, index) =>
            {
                float[] embedding = new float[1024];
                embedding[index % embedding.Length] = 1;
                return (IReadOnlyList<float>)embedding;
            })
            .ToArray();
        return Task.FromResult(new EmbeddingBatch(
            "fake-embedding-model",
            1024,
            embeddings));
    }
}