using ServicePilot.Domain.Reminders;

namespace ServicePilot.Application.Reminders;

public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(
        Guid organizationId,
        Guid reminderId,
        CancellationToken cancellationToken = default);

    Task<Reminder?> GetByAppointmentIdAsync(
        Guid organizationId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> ListAsync(
        Guid organizationId,
        Guid? technicianUserId,
        ReminderStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default);

    void Add(Reminder reminder);
}