namespace ServicePilot.Application.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardSummary> GetSummaryAsync(
        Guid organizationId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken = default);
}