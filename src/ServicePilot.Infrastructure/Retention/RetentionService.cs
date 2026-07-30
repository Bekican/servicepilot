using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Retention;
using ServicePilot.Domain.Users;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Retention;

internal sealed class RetentionService(
    ServicePilotDbContext dbContext)
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

        return new RetentionResult(
            deletedInvitationCount,
            anonymizedAuditLogCount);
    }
}