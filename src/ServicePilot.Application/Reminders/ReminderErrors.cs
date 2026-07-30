using ServicePilot.Application.Common;

namespace ServicePilot.Application.Reminders;

public static class ReminderErrors
{
    public static readonly Error NotFound = new(
        "Reminder.NotFound",
        "Reminder was not found.");

    public static readonly Error InvalidRetry = new(
        "Reminder.InvalidRetry",
        "Only a failed reminder can be retried.");
}