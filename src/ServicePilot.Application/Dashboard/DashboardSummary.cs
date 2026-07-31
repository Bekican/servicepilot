namespace ServicePilot.Application.Dashboard;

public sealed record DashboardSummary(
    DateOnly Date,
    string TimeZoneId,
    int ActiveCustomerCount,
    int ActiveUserCount,
    int ScheduledAppointmentCount,
    int ConfirmedAppointmentCount,
    int InProgressAppointmentCount,
    int CompletedAppointmentCount,
    int CancelledAppointmentCount,
    int FailedReminderCount);