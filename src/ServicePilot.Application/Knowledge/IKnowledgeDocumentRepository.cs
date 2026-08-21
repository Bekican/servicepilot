using ServicePilot.Domain.Knowledge;

namespace ServicePilot.Application.Knowledge;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument?> GetByIdAsync(
        Guid organizationId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        Guid organizationId,
        IReadOnlyCollection<KnowledgeDocumentAccessScope> allowedScopes,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveChecksumExistsAsync(
        Guid organizationId,
        string checksumSha256,
        CancellationToken cancellationToken = default);

    Task<KnowledgeDocumentUsage> GetActiveUsageAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeDocument>> ClaimPendingAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default);

    void Add(KnowledgeDocument document);
}

public sealed record KnowledgeDocumentUsage(
    int DocumentCount,
    long TotalSizeBytes);
