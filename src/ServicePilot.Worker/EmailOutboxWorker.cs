using ServicePilot.Application.Email;
using ServicePilot.Observability;

namespace ServicePilot.Worker;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailOutboxWorker> logger)
    : BackgroundService
{
    private static readonly EventId CycleFailedEvent = new(2003, "EmailOutboxCycleFailed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = ServicePilotTelemetry.StartRootActivity("email_outbox.process_due");
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                EmailOutboxProcessor processor = scope.ServiceProvider
                    .GetRequiredService<EmailOutboxProcessor>();
                int processed = await processor.ProcessDueAsync(cancellationToken: stoppingToken);
                activity?.SetTag("servicepilot.email_outbox.processed_count", processed);
                if (processed > 0)
                    logger.LogInformation("Processed {EmailCount} queued emails", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                ServicePilotTelemetry.RecordFailure(activity, exception);
                logger.LogError(
                    CycleFailedEvent,
                    "Email outbox processing cycle failed with {ExceptionType}",
                    exception.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
