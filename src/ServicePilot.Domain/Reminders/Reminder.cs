namespace ServicePilot.Domain.Reminders;

public sealed class Reminder
{
    public const int MaxRecipientEmailLength = 320;
    public const int MaxErrorLength = 500;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    ];

    private Reminder()
    {
    }

    public Reminder(
        Guid id,
        Guid organizationId,
        Guid appointmentId,
        string? recipientEmail,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(organizationId, nameof(organizationId));
        ValidateIdentifier(appointmentId, nameof(appointmentId));

        string? normalizedRecipient =
            string.IsNullOrWhiteSpace(recipientEmail)
                ? null
                : recipientEmail.Trim().ToLowerInvariant();

        if (normalizedRecipient?.Length
            > MaxRecipientEmailLength)
        {
            throw new ArgumentException(
                "Reminder recipient email is too long",
                nameof(recipientEmail));
        }

        Id = id;
        OrganizationId = organizationId;
        AppointmentId = appointmentId;
        RecipientEmail = normalizedRecipient;
        ScheduledAtUtc = scheduledAtUtc.ToUniversalTime();
        Status = normalizedRecipient is null
            ? ReminderStatus.Skipped
            : ReminderStatus.Pending;
        NextAttemptAtUtc = normalizedRecipient is null
            ? null
            : ScheduledAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public ReminderStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void MarkProcessing(DateTimeOffset attemptedAtUtc)
    {
        if (Status != ReminderStatus.Pending
            && Status != ReminderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only a pending or stale processing reminder can be claimed");
        }

        Status = ReminderStatus.Processing;
        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        ProcessingStartedAtUtc = attemptedAtUtc;
        NextAttemptAtUtc = null;
        UpdatedAtUtc = attemptedAtUtc;
    }

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        EnsureProcessing();
        Status = ReminderStatus.Sent;
        ProcessingStartedAtUtc = null;
        LastError = null;
        UpdatedAtUtc = sentAtUtc;
    }

    public void MarkSkipped(
        DateTimeOffset skippedAtUtc,
        string reason)
    {
        if (Status is ReminderStatus.Sent
            or ReminderStatus.Skipped)
        {
            throw new InvalidOperationException(
                "Terminal reminder cannot be skipped");
        }

        Status = ReminderStatus.Skipped;
        ProcessingStartedAtUtc = null;
        NextAttemptAtUtc = null;
        LastError = SanitizeError(reason);
        UpdatedAtUtc = skippedAtUtc;
    }

    public void MarkDeliveryFailure(
        DateTimeOffset failedAtUtc,
        string sanitizedError)
    {
        EnsureProcessing();

        LastError = SanitizeError(sanitizedError);
        ProcessingStartedAtUtc = null;

        int retryIndex = AttemptCount - 1;

        if (retryIndex < RetryDelays.Length)
        {
            Status = ReminderStatus.Pending;
            NextAttemptAtUtc =
                failedAtUtc + RetryDelays[retryIndex];
        }
        else
        {
            Status = ReminderStatus.Failed;
            NextAttemptAtUtc = null;
        }

        UpdatedAtUtc = failedAtUtc;
    }

    public void RetryManually(DateTimeOffset requestedAtUtc)
    {
        if (Status != ReminderStatus.Failed)
        {
            throw new InvalidOperationException(
                "Only a failed reminder can be retried manually");
        }

        Status = ReminderStatus.Pending;
        AttemptCount = 0;
        NextAttemptAtUtc = requestedAtUtc;
        ProcessingStartedAtUtc = null;
        LastError = null;
        UpdatedAtUtc = requestedAtUtc;
    }

    private void EnsureProcessing()
    {
        if (Status != ReminderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Reminder is not being processed");
        }
    }

    private static string SanitizeError(string value)
    {
        string sanitized = string.IsNullOrWhiteSpace(value)
            ? "Delivery failed"
            : value.Trim();

        return sanitized.Length <= MaxErrorLength
            ? sanitized
            : sanitized[..MaxErrorLength];
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty",
                parameterName);
        }
    }
}