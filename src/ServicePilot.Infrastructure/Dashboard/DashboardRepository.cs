using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Dashboard;
using ServicePilot.Domain.Appointments;
using ServicePilot.Domain.Reminders;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Dashboard;

internal sealed class DashboardRepository(
    ServicePilotDbContext dbContext)
    : IDashboardRepository
{
    public async Task<DashboardSummary> GetSummaryAsync(
        Guid organizationId,
        DateOnly date,
        string timeZoneId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken = default)
    {
        int activeCustomerCount =
            await dbContext.Customers.CountAsync(
                customer =>
                    customer.OrganizationId
                        == organizationId
                    && customer.IsActive,
                cancellationToken);
        int activeUserCount =
            await dbContext.Users.CountAsync(
                user =>
                    user.OrganizationId == organizationId
                    && user.IsActive,
                cancellationToken);

        Dictionary<AppointmentStatus, int>
            appointmentCounts =
            await dbContext.Appointments
                .Where(appointment =>
                    appointment.OrganizationId
                        == organizationId
                    && appointment.StartAtUtc
                        >= dayStartUtc
                    && appointment.StartAtUtc
                        < dayEndUtc)
                .GroupBy(appointment =>
                    appointment.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

        int failedReminderCount =
            await dbContext.Reminders.CountAsync(
                reminder =>
                    reminder.OrganizationId
                        == organizationId
                    && reminder.Status
                        == ReminderStatus.Failed,
                cancellationToken);

        return new DashboardSummary(
            date,
            timeZoneId,
            activeCustomerCount,
            activeUserCount,
            GetCount(
                AppointmentStatus.Scheduled),
            GetCount(
                AppointmentStatus.Confirmed),
            GetCount(
                AppointmentStatus.InProgress),
            GetCount(
                AppointmentStatus.Completed),
            GetCount(
                AppointmentStatus.Cancelled),
            failedReminderCount);

        int GetCount(AppointmentStatus status)
        {
            return appointmentCounts.GetValueOrDefault(
                status);
        }
    }
}