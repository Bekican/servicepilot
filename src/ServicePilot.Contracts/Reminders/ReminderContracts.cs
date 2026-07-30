namespace ServicePilot.Contracts.Reminders;

public sealed record ReminderResponse(
    Guid Id,
    Guid AppointmentId,
    string Status,
    int AttemptCount,
    DateTimeOffset ScheduledAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    string? LastError,
    DateTimeOffset UpdatedAtUtc);