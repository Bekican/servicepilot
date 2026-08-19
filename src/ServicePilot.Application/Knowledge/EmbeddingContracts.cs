namespace ServicePilot.Application.Knowledge;

public interface ITextEmbeddingClient
{
    Task<EmbeddingBatch> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}

public sealed record EmbeddingBatch(
    string Model,
    int Dimensions,
    IReadOnlyList<IReadOnlyList<float>> Embeddings);