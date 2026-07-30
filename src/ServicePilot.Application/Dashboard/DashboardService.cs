using ServicePilot.Application.Abstractions.Tenancy;

namespace ServicePilot.Application.Dashboard;

public sealed class DashboardService(
    ITenantContext tenantContext,
    IDashboardRepository repository,
    TimeProvider timeProvider)
{
    public Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        DateTimeOffset dayStartUtc = new(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        return repository.GetSummaryAsync(
            tenantContext.OrganizationId,
            dayStartUtc,
            dayStartUtc.AddDays(1),
            cancellationToken);
    }
}