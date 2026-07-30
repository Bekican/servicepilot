namespace ServicePilot.Application.Dashboard;

public sealed record DashboardSummary(
    DateOnly DateUtc,
    int ActiveCustomerCount,
    int ActiveUserCount,
    int ScheduledAppointmentCount,
    int ConfirmedAppointmentCount,
    int InProgressAppointmentCount,
    int CompletedAppointmentCount,
    int CancelledAppointmentCount,
    int FailedReminderCount);