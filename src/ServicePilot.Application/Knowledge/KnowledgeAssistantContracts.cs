using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public interface IKnowledgeRetrievalRepository
{
    Task<IReadOnlyList<RetrievedKnowledgeChunk>> SearchAsync(
        Guid organizationId,
        IReadOnlyCollection<KnowledgeDocumentAccessScope> allowedScopes,
        IReadOnlyList<float> queryEmbedding,
        string embeddingModel,
        int embeddingDimensions,
        int candidateCount,
        CancellationToken cancellationToken = default);
}

public interface IGroundedAnswerGenerator
{
    Task<GeneratedGroundedAnswer> GenerateAsync(
        string question,
        IReadOnlyList<GroundingSource> sources,
        CancellationToken cancellationToken = default);
}

public sealed record RetrievedKnowledgeChunk(
    Guid DocumentId,
    string OriginalFileName,
    int PageNumber,
    int ChunkIndex,
    string Content,
    double CosineDistance);

public sealed record GroundingSource(
    string SourceId,
    Guid DocumentId,
    string OriginalFileName,
    int PageNumber,
    string Content);

public sealed record GeneratedGroundedAnswer(
    string Answer,
    IReadOnlyList<string> Citations,
    bool InsufficientEvidence);

public sealed record KnowledgeAnswer(
    string Answer,
    bool InsufficientEvidence,
    IReadOnlyList<KnowledgeCitation> Citations);

public sealed record KnowledgeCitation(
    string SourceId,
    Guid DocumentId,
    string OriginalFileName,
    int PageNumber,
    string ContentUrl);

public sealed class KnowledgeAiUnavailableException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);
