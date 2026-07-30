namespace ServicePilot.Application.Retention;

public interface IRetentionService
{
    Task<RetentionResult> RunAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

public sealed record RetentionResult(
    int DeletedInvitationCount,
    int AnonymizedAuditLogCount);