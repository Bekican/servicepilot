using ServicePilot.Application.Reminders;
using ServicePilot.Observability;

namespace ServicePilot.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger)
    : BackgroundService
{
    private static readonly EventId CycleFailedEvent =
        new(2001, "ReminderCycleFailed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (System.Diagnostics.Activity? activity =
                ServicePilotTelemetry.StartRootActivity(
                    "reminder.process_due"))
            {
                try
                {
                    await using AsyncServiceScope scope =
                        scopeFactory.CreateAsyncScope();
                    ReminderProcessor processor =
                        scope.ServiceProvider.GetRequiredService<
                            ReminderProcessor>();
                    int processed =
                        await processor.ProcessDueAsync(
                            cancellationToken: stoppingToken);
                    activity?.SetTag(
                        "servicepilot.reminder.processed_count",
                        processed);

                    if (processed > 0)
                    {
                        logger.LogInformation(
                            "Processed {ReminderCount} reminders",
                            processed);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    ServicePilotTelemetry.RecordFailure(
                        activity,
                        exception);
                    logger.LogError(
                        CycleFailedEvent,
                        "Reminder processing cycle failed with "
                        + "{ExceptionType}",
                        exception.GetType().Name);
                }
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
