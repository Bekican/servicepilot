using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Organizations;

using ServicePilot.Domain.Organizations;

namespace ServicePilot.Application.Dashboard;

public sealed class DashboardService(
    ITenantContext tenantContext,
    IOrganizationRepository organizationRepository,
    IDashboardRepository repository,
    TimeProvider timeProvider)
{
    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        Organization? organization =
            await organizationRepository.GetByIdAsync(
                tenantContext.OrganizationId,
                cancellationToken);

        if (organization is null)
        {
            throw new InvalidOperationException(
                "Current organization was not found");
        }

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        TimeZoneInfo timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                organization.TimeZoneId);
        DateTimeOffset localNow =
            TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        DateOnly localDate =
            DateOnly.FromDateTime(localNow.DateTime);
        DateTime localDayStart = new(
            localDate.Year,
            localDate.Month,
            localDate.Day,
            0,
            0,
            0,
            DateTimeKind.Unspecified);
        DateTimeOffset dayStartUtc = new(
            TimeZoneInfo.ConvertTimeToUtc(
                localDayStart,
                timeZone),
            TimeSpan.Zero);
        DateTimeOffset dayEndUtc = new(
            TimeZoneInfo.ConvertTimeToUtc(
                localDayStart.AddDays(1),
                timeZone),
            TimeSpan.Zero);

        return await repository.GetSummaryAsync(
            tenantContext.OrganizationId,
            localDate,
            organization.TimeZoneId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);
    }
}