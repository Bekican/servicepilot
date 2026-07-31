namespace ServicePilot.Application.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardSummary> GetSummaryAsync(
        Guid organizationId,
        DateOnly date,
        string timeZoneId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken = default);
}