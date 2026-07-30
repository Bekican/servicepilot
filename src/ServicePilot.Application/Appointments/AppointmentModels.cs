namespace ServicePilot.Application.Appointments;

public sealed record CreateAppointmentData(
    Guid CustomerId,
    Guid ServiceId,
    Guid? TechnicianUserId,
    string StartAt,
    string EndAt);

public sealed record AppointmentResponse(
    Guid Id,
    Guid CustomerId,
    Guid ServiceId,
    Guid? TechnicianUserId,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);