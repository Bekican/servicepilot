namespace ServicePilot.Domain.Reminders;

public enum ReminderStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Skipped
}