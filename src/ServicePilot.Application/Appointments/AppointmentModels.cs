using ServicePilot.Domain.Appointments;

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

public sealed record AppointmentFilterData(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Status,
    Guid? TechnicianUserId);

public sealed record AppointmentDetails(
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
    AppointmentStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AppointmentQuery(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    AppointmentStatus? Status,
    Guid? TechnicianUserId);