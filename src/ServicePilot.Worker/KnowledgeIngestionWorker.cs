using ServicePilot.Application.Knowledge;
using ServicePilot.Observability;

namespace ServicePilot.Worker;

public sealed class KnowledgeIngestionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<KnowledgeIngestionWorker> logger)
    : BackgroundService
{
    private static readonly EventId CycleFailedEvent =
        new(2004, "KnowledgeIngestionCycleFailed");

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using System.Diagnostics.Activity? activity =
                ServicePilotTelemetry.StartRootActivity(
                    "knowledge.process_pending");
            try
            {
                await using AsyncServiceScope scope =
                    scopeFactory.CreateAsyncScope();
                KnowledgeDocumentIngestionProcessor processor =
                    scope.ServiceProvider.GetRequiredService<
                        KnowledgeDocumentIngestionProcessor>();
                int processed = await processor.ProcessPendingAsync(
                    cancellationToken: stoppingToken);
                activity?.SetTag(
                    "servicepilot.knowledge.processed_count",
                    processed);

                if (processed > 0)
                {
                    logger.LogInformation(
                        "Processed {DocumentCount} knowledge documents",
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
                ServicePilotTelemetry.RecordFailure(activity, exception);
                logger.LogError(
                    CycleFailedEvent,
                    "Knowledge processing cycle failed with {ExceptionType}",
                    exception.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}