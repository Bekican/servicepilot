using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Retention;
using ServicePilot.Application.Knowledge;
using ServicePilot.Domain.Knowledge;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Retention;

internal sealed class RetentionService(
    ServicePilotDbContext dbContext,
    IKnowledgeDocumentStorage knowledgeStorage)
    : IRetentionService
{
    public async Task<RetentionResult> RunAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset invitationCutoff =
            nowUtc - TimeSpan.FromDays(30);
        DateTimeOffset auditCutoff =
            nowUtc.AddYears(-5);

        int deletedInvitationCount =
            await dbContext.UserInvitations
                .Where(invitation =>
                    invitation.Status
                        == UserInvitationStatus.Accepted
                    && invitation.AcceptedAtUtc
                        < invitationCutoff
                    || invitation.Status
                        == UserInvitationStatus.Revoked
                    && invitation.RevokedAtUtc
                        < invitationCutoff
                    || invitation.Status
                        == UserInvitationStatus.Pending
                    && invitation.ExpiresAtUtc
                        < invitationCutoff)
                .ExecuteDeleteAsync(cancellationToken);

        int anonymizedAuditLogCount =
            await dbContext.AuditLogs
                .Where(auditLog =>
                    auditLog.OccurredAtUtc < auditCutoff
                    && auditLog.AnonymizedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            auditLog =>
                                auditLog.ActorUserId,
                            (Guid?)null)
                        .SetProperty(
                            auditLog =>
                                auditLog.Metadata,
                            (string?)null)
                        .SetProperty(
                            auditLog =>
                                auditLog.AnonymizedAtUtc,
                            nowUtc),
                cancellationToken);

        DateTimeOffset knowledgeCutoff = nowUtc - TimeSpan.FromDays(7);
        var deletedDocuments = await dbContext.KnowledgeDocuments
            .AsNoTracking()
            .Where(document =>
                document.Status == KnowledgeDocumentStatus.Deleted
                && document.UpdatedAtUtc < knowledgeCutoff)
            .OrderBy(document => document.UpdatedAtUtc)
            .ThenBy(document => document.Id)
            .Take(100)
            .Select(document => new
            {
                document.Id,
                document.StorageKey
            })
            .ToArrayAsync(cancellationToken);
        List<Guid> purgeableDocumentIds = [];
        foreach (var document in deletedDocuments)
        {
            try
            {
                await knowledgeStorage.DeleteAsync(
                    document.StorageKey,
                    cancellationToken);
                purgeableDocumentIds.Add(document.Id);
            }
            catch (FileNotFoundException)
            {
                purgeableDocumentIds.Add(document.Id);
            }
            catch (DirectoryNotFoundException)
            {
                purgeableDocumentIds.Add(document.Id);
            }
            catch (IOException)
            {
                // Keep the database row so the next retention cycle retries.
            }
            catch (UnauthorizedAccessException)
            {
                // Keep the database row so the next retention cycle retries.
            }
        }

        int purgedKnowledgeDocumentCount = purgeableDocumentIds.Count == 0
            ? 0
            : await dbContext.KnowledgeDocuments
                .Where(document =>
                    purgeableDocumentIds.Contains(document.Id)
                    && document.Status == KnowledgeDocumentStatus.Deleted)
                .ExecuteDeleteAsync(cancellationToken);

        return new RetentionResult(
            deletedInvitationCount,
            anonymizedAuditLogCount,
            purgedKnowledgeDocumentCount);
    }
}
