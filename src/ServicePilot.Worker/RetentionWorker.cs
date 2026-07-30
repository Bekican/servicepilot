using ServicePilot.Application.Retention;

namespace ServicePilot.Worker;

public sealed class RetentionWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RetentionWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope =
                    scopeFactory.CreateAsyncScope();
                IRetentionService retentionService =
                    scope.ServiceProvider.GetRequiredService<
                        IRetentionService>();
                RetentionResult result =
                    await retentionService.RunAsync(
                        timeProvider.GetUtcNow(),
                        stoppingToken);

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
                    exception,
                    "Retention cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromHours(24),
                stoppingToken);
        }
    }
}