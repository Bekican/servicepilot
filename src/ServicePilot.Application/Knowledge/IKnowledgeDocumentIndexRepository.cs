using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public interface IKnowledgeDocumentIndexRepository
{
    Task PublishAsync(
        KnowledgeDocument document,
        IReadOnlyList<KnowledgeChunkIndexEntry> chunks,
        string embeddingModel,
        int embeddingDimensions,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed record KnowledgeChunkIndexEntry(
    Guid Id,
    int ChunkIndex,
    int PageNumber,
    string Content,
    IReadOnlyList<float> Embedding);