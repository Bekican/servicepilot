namespace ServicePilot.Contracts.Dashboard;

public sealed record DashboardSummaryResponse(
    DateOnly DateUtc,
    int ActiveCustomerCount,
    int ActiveUserCount,
    int ScheduledAppointmentCount,
    int ConfirmedAppointmentCount,
    int InProgressAppointmentCount,
    int CompletedAppointmentCount,
    int CancelledAppointmentCount,
    int FailedReminderCount);