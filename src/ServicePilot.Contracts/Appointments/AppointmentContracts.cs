namespace ServicePilot.Contracts.Appointments;

public sealed record CreateAppointmentRequest(
    Guid CustomerId,
    Guid ServiceId,
    Guid? TechnicianUserId,
    string StartAt,
    string EndAt);

public sealed record AssignTechnicianRequest(
    Guid TechnicianUserId);

public sealed record AppointmentStatusRequest(
    string Status);

public sealed record AppointmentResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerNumber,
    string CustomerDisplayName,
    Guid ServiceId,
    string ServiceName,
    Guid? TechnicianUserId,
    string? TechnicianDisplayName,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string Status,
    IReadOnlyList<string> AllowedTransitions,
    bool CanAssignTechnician,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);