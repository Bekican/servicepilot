using ServicePilot.Domain.Reminders;

namespace ServicePilot.UnitTests.Reminders;

public sealed class ReminderTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MissingEmail_ShouldCreateSkippedReminder()
    {
        Reminder reminder = Create(null);

        Assert.Equal(
            ReminderStatus.Skipped,
            reminder.Status);
        Assert.Null(reminder.NextAttemptAtUtc);
    }

    [Fact]
    public void DeliveryFailures_ShouldUseDurableRetrySchedule()
    {
        Reminder reminder = Create("customer@example.com");

        DateTimeOffset[] expectedRetryTimes =
        [
            UtcNow.AddMinutes(1),
            UtcNow.AddMinutes(6),
            UtcNow.AddMinutes(36)
        ];

        for (int index = 0; index < 3; index++)
        {
            reminder.MarkProcessing(UtcNow);
            reminder.MarkDeliveryFailure(
                index == 0
                    ? UtcNow
                    : expectedRetryTimes[index - 1],
                "Email delivery failed");

            Assert.Equal(
                ReminderStatus.Pending,
                reminder.Status);
            Assert.Equal(
                expectedRetryTimes[index],
                reminder.NextAttemptAtUtc);
        }

        reminder.MarkProcessing(UtcNow);
        reminder.MarkDeliveryFailure(
            expectedRetryTimes[^1],
            "Email delivery failed");

        Assert.Equal(
            ReminderStatus.Failed,
            reminder.Status);
        Assert.Null(reminder.NextAttemptAtUtc);
    }

    [Fact]
    public void ManualRetry_ShouldResetFailedDeliveryCycle()
    {
        Reminder reminder = Create("customer@example.com");

        for (int index = 0; index < 4; index++)
        {
            reminder.MarkProcessing(UtcNow);
            reminder.MarkDeliveryFailure(
                UtcNow,
                "Email delivery failed");
        }

        reminder.RetryManually(UtcNow.AddHours(1));

        Assert.Equal(
            ReminderStatus.Pending,
            reminder.Status);
        Assert.Equal(0, reminder.AttemptCount);
        Assert.Null(reminder.LastError);
        Assert.Equal(
            UtcNow.AddHours(1),
            reminder.NextAttemptAtUtc);
    }

    private static Reminder Create(string? email)
    {
        return new Reminder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            email,
            UtcNow,
            UtcNow);
    }
}