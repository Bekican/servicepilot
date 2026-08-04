using ServicePilot.Application.Retention;
using ServicePilot.Observability;

namespace ServicePilot.Worker;

public sealed class RetentionWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RetentionWorker> logger)
    : BackgroundService
{
    private static readonly EventId CycleFailedEvent =
        new(2101, "RetentionCycleFailed");

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using System.Diagnostics.Activity? activity =
                    ServicePilotTelemetry.StartActivity(
                        "retention.run");
                await using AsyncServiceScope scope =
                    scopeFactory.CreateAsyncScope();
                IRetentionService retentionService =
                    scope.ServiceProvider.GetRequiredService<
                        IRetentionService>();
                RetentionResult result =
                    await retentionService.RunAsync(
                        timeProvider.GetUtcNow(),
                        stoppingToken);
                activity?.SetTag(
                    "servicepilot.retention.deleted_invitation_count",
                    result.DeletedInvitationCount);
                activity?.SetTag(
                    "servicepilot.retention.anonymized_audit_count",
                    result.AnonymizedAuditLogCount);

                logger.LogInformation(
                    "Retention completed: {InvitationCount} "
                    + "invitations deleted, {AuditCount} "
                    + "audit logs anonymized",
                    result.DeletedInvitationCount,
                    result.AnonymizedAuditLogCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    CycleFailedEvent,
                    "Retention cycle failed with "
                    + "{ExceptionType}",
                    exception.GetType().Name);
            }

            await Task.Delay(
                TimeSpan.FromHours(24),
                stoppingToken);
        }
    }
}
