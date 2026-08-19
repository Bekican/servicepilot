using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class KnowledgeDocumentRepository(
    ServicePilotDbContext dbContext)
    : IKnowledgeDocumentRepository
{
    public Task<KnowledgeDocument?> GetByIdAsync(
        Guid organizationId,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        dbContext.KnowledgeDocuments.SingleOrDefaultAsync(
            document =>
                document.OrganizationId == organizationId
                && document.Id == documentId,
            cancellationToken);

    public async Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        Guid organizationId,
        IReadOnlyCollection<KnowledgeDocumentAccessScope> allowedScopes,
        CancellationToken cancellationToken = default) =>
        await dbContext.KnowledgeDocuments
            .AsNoTracking()
            .Where(document =>
                document.OrganizationId == organizationId
                && document.Status != KnowledgeDocumentStatus.Deleted
                && allowedScopes.Contains(document.AccessScope))
            .OrderByDescending(document => document.CreatedAtUtc)
            .ThenBy(document => document.Id)
            .ToArrayAsync(cancellationToken);

    public Task<bool> ActiveChecksumExistsAsync(
        Guid organizationId,
        string checksumSha256,
        CancellationToken cancellationToken = default) =>
        dbContext.KnowledgeDocuments.AnyAsync(
            document =>
                document.OrganizationId == organizationId
                && document.ChecksumSha256 == checksumSha256
                && document.Status != KnowledgeDocumentStatus.Deleted,
            cancellationToken);

    public async Task<IReadOnlyList<KnowledgeDocument>> ClaimPendingAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        int boundedBatchSize = Math.Clamp(batchSize, 1, 20);
        await dbContext.KnowledgeDocuments
            .Where(document =>
                document.Status == KnowledgeDocumentStatus.Processing
                && document.ProcessingStartedAtUtc <= staleBeforeUtc
                && document.ProcessingAttemptCount
                    >= KnowledgeDocument.MaxProcessingAttempts)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        document => document.Status,
                        KnowledgeDocumentStatus.Failed)
                    .SetProperty(
                        document => document.ProcessingStartedAtUtc,
                        (DateTimeOffset?)null)
                    .SetProperty(
                        document => document.LastErrorCode,
                        "KnowledgeDocument.ProcessingLeaseExpired")
                    .SetProperty(
                        document => document.LastErrorMessage,
                        "Document processing did not complete after three attempts.")
                    .SetProperty(
                        document => document.UpdatedAtUtc,
                        nowUtc),
                cancellationToken);

        return await dbContext.KnowledgeDocuments
            .FromSqlInterpolated($"""
                WITH pending AS (
                    SELECT id
                    FROM knowledge_documents
                    WHERE status = 'Pending'
                       OR (
                           status = 'Processing'
                           AND processing_started_at_utc <= {staleBeforeUtc}
                           AND processing_attempt_count
                               < {KnowledgeDocument.MaxProcessingAttempts}
                       )
                    ORDER BY created_at_utc, id
                    FOR UPDATE SKIP LOCKED
                    LIMIT {boundedBatchSize}
                )
                UPDATE knowledge_documents AS document
                SET status = 'Processing',
                    processing_attempt_count =
                        document.processing_attempt_count + 1,
                    processing_started_at_utc = {nowUtc},
                    last_error_code = NULL,
                    last_error_message = NULL,
                    updated_at_utc = {nowUtc}
                FROM pending
                WHERE document.id = pending.id
                RETURNING document.*
                """)
            .AsTracking()
            .ToArrayAsync(cancellationToken);
    }

    public void Add(KnowledgeDocument document) =>
        dbContext.KnowledgeDocuments.Add(document);
}
