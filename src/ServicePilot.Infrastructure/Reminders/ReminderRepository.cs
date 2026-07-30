using Microsoft.EntityFrameworkCore;

using ServicePilot.Application.Reminders;
using ServicePilot.Domain.Reminders;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.Infrastructure.Reminders;

internal sealed class ReminderRepository(
    ServicePilotDbContext dbContext)
    : IReminderRepository
{
    public Task<Reminder?> GetByIdAsync(
        Guid organizationId,
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Reminders.SingleOrDefaultAsync(
            reminder =>
                reminder.OrganizationId == organizationId
                && reminder.Id == reminderId,
            cancellationToken);
    }

    public Task<Reminder?> GetByAppointmentIdAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Reminders.SingleOrDefaultAsync(
            reminder =>
                reminder.OrganizationId == organizationId
                && reminder.AppointmentId == appointmentId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> ListAsync(
        Guid organizationId,
        Guid? technicianUserId,
        ReminderStatus? status,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Reminders
            .AsNoTracking()
            .Where(reminder =>
                reminder.OrganizationId == organizationId
                && (
                    status == null
                    || reminder.Status == status
                )
                && (
                    technicianUserId == null
                    || dbContext.Appointments.Any(
                        appointment =>
                            appointment.OrganizationId
                                == organizationId
                            && appointment.Id
                                == reminder.AppointmentId
                            && appointment.TechnicianUserId
                                == technicianUserId)
                ))
            .OrderByDescending(reminder =>
                reminder.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        int boundedBatchSize = Math.Clamp(
            batchSize,
            1,
            100);

        return await dbContext.Reminders
            .FromSqlInterpolated($"""
                WITH due AS (
                    SELECT id
                    FROM reminders
                    WHERE (
                        status = 'Pending'
                        AND next_attempt_at_utc <= {nowUtc}
                    ) OR (
                        status = 'Processing'
                        AND processing_started_at_utc
                            <= {staleBeforeUtc}
                    )
                    ORDER BY next_attempt_at_utc NULLS FIRST
                    FOR UPDATE SKIP LOCKED
                    LIMIT {boundedBatchSize}
                )
                UPDATE reminders AS reminder
                SET status = 'Processing',
                    attempt_count =
                        reminder.attempt_count + 1,
                    last_attempt_at_utc = {nowUtc},
                    processing_started_at_utc = {nowUtc},
                    next_attempt_at_utc = NULL,
                    updated_at_utc = {nowUtc}
                FROM due
                WHERE reminder.id = due.id
                RETURNING reminder.*
                """)
            .AsTracking()
            .ToArrayAsync(cancellationToken);
    }

    public void Add(Reminder reminder)
    {
        dbContext.Reminders.Add(reminder);
    }
}