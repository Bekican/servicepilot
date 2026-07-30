using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Reminders;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Reminders;

public sealed class ReminderManagementService(
    ICurrentUserContext currentUser,
    IReminderRepository reminderRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ReminderResponse>> ListAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        ReminderStatus? statusFilter = Enum.TryParse(
            status,
            true,
            out ReminderStatus parsedStatus)
            && Enum.IsDefined(parsedStatus)
                ? parsedStatus
                : null;

        Guid? technicianFilter =
            currentUser.Role == UserRoles.Technician
                ? currentUser.UserId
                : null;

        IReadOnlyList<Reminder> reminders =
            await reminderRepository.ListAsync(
                currentUser.OrganizationId,
                technicianFilter,
                statusFilter,
                cancellationToken);

        return reminders.Select(Map).ToArray();
    }

    public async Task<Result<ReminderResponse>> RetryAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        Reminder? reminder =
            await reminderRepository.GetByIdAsync(
                currentUser.OrganizationId,
                reminderId,
                cancellationToken);

        if (reminder is null)
        {
            return Result<ReminderResponse>.Failure(
                ReminderErrors.NotFound);
        }

        try
        {
            reminder.RetryManually(
                timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException)
        {
            return Result<ReminderResponse>.Failure(
                ReminderErrors.InvalidRetry);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ReminderResponse>.Success(
            Map(reminder));
    }

    internal static ReminderResponse Map(Reminder reminder)
    {
        return new ReminderResponse(
            reminder.Id,
            reminder.AppointmentId,
            reminder.Status.ToString(),
            reminder.AttemptCount,
            reminder.ScheduledAtUtc,
            reminder.NextAttemptAtUtc,
            reminder.LastAttemptAtUtc,
            reminder.LastError,
            reminder.UpdatedAtUtc);
    }
}