using ServicePilot.Application.Reminders;

namespace ServicePilot.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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
                logger.LogError(
                    exception,
                    "Reminder processing cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
    }
}